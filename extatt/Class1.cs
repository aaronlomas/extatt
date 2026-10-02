using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using acapp = Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.ApplicationServices;

namespace extatt
{
    public class Registro
    {
        public ObjectId Ename;
        public string Tipo = "";
        public double X;
        public double Y;
        public double? Px;
        public double? Py;
        public int In;
        public int Out;
        public int ToggleInOut;
        public ObjectId? Circulo;
    }

    public class Class1
    {
        private const string LayerPline = "EXT-ATT-PLINE";
        private const string LayerCircle = "EXT-ATT-CIRCLE";
        private const string TagE = "E-01";
        private const string TagSc = "SC/H=9M";
        private const string TagElec = "01/ELEC/--";
        private const string TagNum = "NUMERACION";
        private const string TagFat = "FAT(01)";
        private const double CircleRadius = 7.0;
        private const short RedAci = 1;

        private static Database Db
        {
            get { return HostApplicationServices.WorkingDatabase; }
        }

        // ================================================================
        // UTILIDADES GENERALES Y LECTURA DE ATRIBUTOS
        // ================================================================

        private static string Str(string? v)
        {
            return v ?? "";
        }

        private static string Str(double v)
        {
            return Rtos(v);
        }

        private static string Str(int v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private static string Rtos(double v)
        {
            string s = v.ToString("0.0###", CultureInfo.InvariantCulture);
            if (s.IndexOf('.') >= 0)
            {
                s = s.TrimEnd('0');
                if (s.EndsWith(".", StringComparison.Ordinal))
                    s += "0";
            }
            return s;
        }

        private static bool KwEs(string? resultado, string alias, string nombre)
        {
            return string.Equals(resultado, alias, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(resultado, nombre, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetValAttr(ObjectId ename, string tag)
        {
            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                if (!ename.IsValid || ename.IsErased)
                    return "";

                if (!(tr.GetObject(ename, OpenMode.ForRead) is BlockReference br))
                    return "";

                foreach (ObjectId attId in br.AttributeCollection)
                {
                    if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference ar))
                        continue;
                    if (string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                        return Str(ar.TextString);
                }
            }
            return "";
        }

        // ================================================================
        // RUTAS - CARPETA DESTINO Y NOMBRE DE ARCHIVO CSV
        // ================================================================

        private static string CarpetaDestino()
        {
            string? up = Environment.GetEnvironmentVariable("USERPROFILE");
            string? od = Environment.GetEnvironmentVariable("OneDrive");
            string? ruta = null;

            List<string> cand = new List<string>();
            if (!string.IsNullOrEmpty(up))
                cand.Add(up + "\\Desktop");
            if (!string.IsNullOrEmpty(od))
                cand.Add(od + "\\Desktop");
            if (!string.IsNullOrEmpty(up))
                cand.Add(up + "\\OneDrive\\Desktop");

            foreach (string c in cand)
            {
                if (ruta == null && Directory.Exists(c))
                    ruta = c + "\\";
            }

            if (ruta == null && !string.IsNullOrEmpty(up))
                ruta = up + "\\Desktop\\";

            if (ruta == null)
            {
                string? dwg = acapp.Application.GetSystemVariable("DWGPREFIX") as string;
                if (!string.IsNullOrEmpty(dwg))
                    ruta = dwg;
            }

            if (ruta != null && !ruta.EndsWith("\\", StringComparison.Ordinal))
                ruta += "\\";

            return ruta ?? "";
        }

        private static string ArchivoNuevo(string carpeta)
        {
            int i = 0;
            string archivo;
            do
            {
                i++;
                archivo = carpeta + "data_extatt_" + (i < 10 ? "0" : "") +
                          i.ToString(CultureInfo.InvariantCulture) + ".csv";
            }
            while (File.Exists(archivo));
            return archivo;
        }

        private static string RutaCsv()
        {
            return ArchivoNuevo(CarpetaDestino());
        }

        // ================================================================
        // DIBUJO DE POLILINEA ROJA
        // ================================================================

        private static void Caplina(string nombre)
        {
            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(nombre))
                {
                    LayerTable ltW = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    LayerTableRecord layer = new LayerTableRecord
                    {
                        Name = nombre,
                        LinetypeObjectId = db.ContinuousLinetype,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, RedAci)
                    };
                    ltW.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                else
                {
                    LayerTableRecord layer =
                        (LayerTableRecord)tr.GetObject(lt[nombre], OpenMode.ForRead);
                    if (Math.Abs(layer.Color.ColorIndex) != RedAci)
                    {
                        layer.UpgradeOpen();
                        layer.Color = Color.FromColorIndex(ColorMethod.ByAci, RedAci);
                    }
                }
                tr.Commit();
            }
        }

        private static ObjectId? DibujarPline(List<Registro> dataList, ObjectId? pline)
        {
            Caplina(LayerPline);

            List<Point2d> pts = new List<Point2d>();
            foreach (Registro reg in dataList)
            {
                double px = reg.Px ?? reg.X;
                double py = reg.Py ?? reg.Y;
                pts.Add(new Point2d(px, py));
            }
            int n = pts.Count;

            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline? pl = null;
                if (pline.HasValue && pline.Value.IsValid && !pline.Value.IsErased)
                    pl = tr.GetObject(pline.Value, OpenMode.ForWrite) as Polyline;

                if (n > 1)
                {
                    if (pl == null)
                    {
                        pl = new Polyline();
                        pl.Layer = LayerPline;
                        pl.ColorIndex = RedAci;
                        BlockTableRecord space =
                            (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        space.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                        pline = pl.ObjectId;
                    }
                    while (pl.NumberOfVertices > n)
                        pl.RemoveVertexAt(pl.NumberOfVertices - 1);
                    for (int i = pl.NumberOfVertices; i < n; i++)
                        pl.AddVertexAt(i, pts[i], 0.0, 0.0, 0.0);
                    for (int i = 0; i < n; i++)
                        pl.SetPointAt(i, pts[i]);
                    tr.Commit();
                    return pline;
                }

                if (pl != null)
                    pl.Erase();

                tr.Commit();
                return null;
            }
        }

        // ================================================================
        // DIBUJO DE CIRCULO ROJO (FAT)
        // ================================================================

        private static ObjectId DibujarCirculo(Point3d pt)
        {
            Caplina(LayerCircle);

            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Circle circle = new Circle(new Point3d(pt.X, pt.Y, 0.0), Vector3d.ZAxis, CircleRadius)
                {
                    Layer = LayerCircle,
                    ColorIndex = RedAci
                };
                BlockTableRecord space =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
                tr.Commit();
                return circle.ObjectId;
            }
        }

        private static void BorrarCirculo(ObjectId? id)
        {
            if (!id.HasValue || !id.Value.IsValid || id.Value.IsErased)
                return;

            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBObject obj = tr.GetObject(id.Value, OpenMode.ForWrite);
                obj.Erase();
                tr.Commit();
            }
        }

        // ================================================================
        // PROCESAMIENTO DE ENTIDADES SELECCIONADAS
        // ================================================================

        private static Registro? Procesar(ObjectId ename, Point3d? pick)
        {
            double? px = pick?.X;
            double? py = pick?.Y;

            Database db = Db;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                if (!ename.IsValid || ename.IsErased)
                    return null;

                DBObject obj = tr.GetObject(ename, OpenMode.ForRead);
                ObjectId padre = obj is AttributeReference ar ? ar.OwnerId : ename;

                if (padre.IsNull || padre.IsErased)
                    return null;

                if (!(tr.GetObject(padre, OpenMode.ForRead) is BlockReference br))
                    return null;

                Point3d pt = br.Position;
                return new Registro
                {
                    Ename = padre,
                    Tipo = "INSERT",
                    X = pt.X,
                    Y = pt.Y,
                    Px = px,
                    Py = py,
                    In = 0,
                    Out = 0,
                    ToggleInOut = 0
                };
            }
        }

        // ================================================================
        // EXPORTACION A CSV
        // ================================================================

        private static void Exportar(List<Registro> dataList, Editor ed)
        {
            if (dataList.Count == 0)
            {
                ed.WriteMessage("\n[ERROR] Seleccion vacia. No se exporto nada.\n");
                return;
            }

            string ruta = RutaCsv();
            StreamWriter? fp = null;
            string seguir = "R";

            while (seguir == "R" && fp == null)
            {
                try
                {
                    fp = new StreamWriter(ruta, false, new UTF8Encoding(false));
                }
                catch (IOException)
                {
                    fp = null;
                }
                catch (UnauthorizedAccessException)
                {
                    fp = null;
                }

                if (fp == null)
                {
                    ed.WriteMessage("\n[ERROR] No se pudo escribir en:\n        " + ruta + "\n");
                    ed.WriteMessage("El archivo puede estar abierto en Excel.\n");
                    PromptKeywordOptions kwo =
                        new PromptKeywordOptions("\nCerrar Excel y reintentar o salir? ")
                        {
                            AllowNone = false
                        };
                    kwo.Keywords.Add("R", "R", "[R]eintentar");
                    kwo.Keywords.Add("S", "S", "[S]alir");
                    PromptResult kr = ed.GetKeywords(kwo);
                    if (kr.Status == PromptStatus.Error ||
                        kr.Status == PromptStatus.Cancel ||
                        !KwEs(kr.StringResult, "R", "Reintentar"))
                        seguir = "S";
                }
            }

            if (fp == null)
            {
                ed.WriteMessage("\nExportacion cancelada. No se escribio nada.\n");
                return;
            }

            using (fp)
            {
                fp.WriteLine(
                    "No.;Codigo_Poste;Estructura;descripcion;FAT;IN;OUT;Coordenada_X;Coordenada_Y");

                int idx = 0;
                foreach (Registro item in dataList)
                {
                    idx++;
                    string valE = GetValAttr(item.Ename, TagE);
                    string valSc = GetValAttr(item.Ename, TagSc);
                    string valElec = GetValAttr(item.Ename, TagElec);
                    string valNum = GetValAttr(item.Ename, TagNum);
                    string valFat = GetValAttr(item.Ename, TagFat);

                    double? cx = item.X;
                    double? cy = item.Y;
                    if (valE == "" && valSc == "" && valElec == "" && valNum == "")
                    {
                        cx = null;
                        cy = null;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(idx).Append(';');
                    sb.Append(Str(valE)).Append(Str(valNum)).Append(';');
                    sb.Append(Str(valSc)).Append(';');
                    sb.Append(Str(valElec)).Append(';');
                    sb.Append(Str(valFat)).Append(';');
                    sb.Append(Str(item.In)).Append(';');
                    sb.Append(Str(item.Out)).Append(';');
                    sb.Append(cx.HasValue ? Str(cx.Value) : "").Append(';');
                    sb.Append(cy.HasValue ? Str(cy.Value) : "");
                    fp.WriteLine(sb.ToString());
                }
            }

            ed.WriteMessage("\n================================================");
            ed.WriteMessage("\nREPORTE GENERADO CON EXITO!");
            ed.WriteMessage("\nArchivo guardado en:\n        " + ruta);
            ed.WriteMessage("\n================================================\n");
        }

        // ================================================================
        // COMANDO PRINCIPAL: EXT-ATT
        // ================================================================

        private static void KeywordDeshacer(Editor ed, List<Registro> dataList,
                                           ref ObjectId? pline, ref int toggleInOut)
        {
            if (dataList.Count > 0)
            {
                int idx = dataList.Count;
                Registro last = dataList[idx - 1];
                string valE = GetValAttr(last.Ename, TagE);
                string valNum = GetValAttr(last.Ename, TagNum);
                string valFat = GetValAttr(last.Ename, TagFat);
                toggleInOut = last.ToggleInOut;

                ed.WriteMessage("- Eliminado: #" + Str(idx) +
                                " - BLQ: " + Str(valE) + Str(valNum) + Str(valFat) + "\n");

                BorrarCirculo(last.Circulo);
                dataList.RemoveAt(dataList.Count - 1);
                pline = DibujarPline(dataList, pline);
            }
            else
            {
                ed.WriteMessage("\nNo hay registros para eliminar.\n");
            }
        }

        private static void KeywordInOut(Editor ed, List<Registro> dataList, ref int toggleInOut)
        {
            if (dataList.Count > 0)
            {
                Registro lastReg = dataList[dataList.Count - 1];
                string valFat = GetValAttr(lastReg.Ename, TagFat);
                if (!string.IsNullOrEmpty(valFat) && valFat != " ")
                {
                    if (toggleInOut == 0)
                    {
                        lastReg.In += 10;
                        lastReg.ToggleInOut = 1;
                        ed.WriteMessage("\n>> IN: +10 (Total: " + Str(lastReg.In) + ") [FAT]\n");
                    }
                    else
                    {
                        lastReg.Out += 10;
                        lastReg.ToggleInOut = 0;
                        ed.WriteMessage("\n>> OUT: +10 (Total: " + Str(lastReg.Out) + ") [FAT]\n");
                    }
                    toggleInOut = 1 - toggleInOut;
                }
                else
                {
                    ed.WriteMessage(
                        "\nEl ultimo bloque no tiene FAT. Seleccione uno con FAT primero.\n");
                }
            }
            else
            {
                ed.WriteMessage("\nNo hay registros. Seleccione un bloque primero.\n");
            }
        }

        private static bool entidadSeleccionada(Editor ed, Database db, ObjectId oid,
                                                Point3d pick, List<Registro> dataList,
                                                ref ObjectId? pline, ref int toggleInOut)
        {
            bool isPline;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                if (!oid.IsValid || oid.IsErased)
                    return false;
                isPline = tr.GetObject(oid, OpenMode.ForRead) is Polyline;
            }

            if (isPline)
            {
                ed.WriteMessage("\nEvite la polilinea roja: pulse sobre el bloque.\n");
                return false;
            }

            Registro? reg = Procesar(oid, pick);
            if (reg == null)
            {
                ed.WriteMessage("\n[ERROR] Seleccion invalida.\n");
                return false;
            }

            dataList.Add(reg);
            pline = DibujarPline(dataList, pline);

            string rE = GetValAttr(reg.Ename, TagE);
            string rNum = GetValAttr(reg.Ename, TagNum);
            string rFat = GetValAttr(reg.Ename, TagFat);

            ed.WriteMessage("#" + Str(dataList.Count) +
                            " -> BLQ: " + Str(rE) + Str(rNum) + Str(rFat) + "\n");

            if (!string.IsNullOrEmpty(rFat) && rFat != " ")
            {
                reg.Circulo = DibujarCirculo(new Point3d(reg.X, reg.Y, 0.0));
                ed.WriteMessage("  [FAT detectado] Circulo rojo dibujado.\n");
                toggleInOut = 0;
            }

            return true;
        }

        [CommandMethod("EXT-ATT")]
        public void ExtAtt()
        {
            Document doc = acapp.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            List<Registro> dataList = new List<Registro>();
            ObjectId? pline = null;
            int toggleInOut = 0;

            ed.WriteMessage("\n------------------------------------------\n");
            ed.WriteMessage("  Extrae Values de bloques y se exporta a excel\n");
            ed.WriteMessage("  ENTER para terminar, Z para invalidar ultimo.\n");
            ed.WriteMessage("  Presione 1 para IN/OUT en FAT (+10).\n");
            ed.WriteMessage("--------------------------------------------\n\n");

            PromptEntityOptions peo = new PromptEntityOptions("\nSeleccione un bloque: ");
            peo.Keywords.Add("Z", "Z", "Deshacer el ultimo registro");
            peo.Keywords.Add("1", "1", "Sumar 10 a IN o OUT en FAT");

            while (true)
            {
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK && per.Status != PromptStatus.Keyword)
                    break;

                if (per.Status == PromptStatus.Keyword)
                {
                    string kw = per.StringResult;

                    if (KwEs(kw, "Z", "Deshacer"))
                        KeywordDeshacer(ed, dataList, ref pline, ref toggleInOut);
                    else if (KwEs(kw, "1", "IN/OUT"))
                        KeywordInOut(ed, dataList, ref toggleInOut);

                    continue;
                }

                entidadSeleccionada(ed, db, per.ObjectId, per.PickedPoint,
                                     dataList, ref pline, ref toggleInOut);
            }

            if (dataList.Count > 0)
                Exportar(dataList, ed);
            else
                ed.WriteMessage("\n[ERROR] Seleccion vacia. No se exporto nada.\n");
        }
    }
}