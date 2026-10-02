using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using acapp = Autodesk.AutoCAD.ApplicationServices.Core;

namespace extatt
{
    public class ComandoExtAtt
    {
        [CommandMethod("EXT-ATT")]
        public void Ejecutar()
        {
            Document doc = acapp.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Sesion sesion = new Sesion();

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

                    if (Texto.KwEs(kw, "Z", "Deshacer"))
                        KeywordDeshacer(db, ed, sesion);
                    else if (Texto.KwEs(kw, "1", "IN/OUT"))
                        KeywordInOut(db, ed, sesion);

                    continue;
                }

                EntidadSeleccionada(db, ed, per.ObjectId, per.PickedPoint, sesion);
            }

            if (sesion.Registros.Count > 0)
                ExportadorCsv.Exportar(db, sesion.Registros, ed);
            else
                ed.WriteMessage("\n[ERROR] Seleccion vacia. No se exporto nada.\n");
        }

        public static bool EntidadSeleccionada(Database db, Editor ed, ObjectId oid,
                                               Point3d pick, Sesion sesion)
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

            Registro? reg = Procesador.Procesar(db, oid, pick);
            if (reg == null)
            {
                ed.WriteMessage("\n[ERROR] Seleccion invalida.\n");
                return false;
            }

            sesion.Registros.Add(reg);
            sesion.Polilinea = Dibujo.Polilinea(db, sesion.Registros, sesion.Polilinea);

            ValoresBloque v = Atributos.Leer(db, reg.Ename);

            ed.WriteMessage("#" + Texto.Str(sesion.Registros.Count) +
                            " -> BLQ: " + v.Codigo + v.Fat + "\n");

            if (!string.IsNullOrEmpty(v.Fat) && v.Fat != " ")
            {
                reg.Circulo = Dibujo.Circulo(db, new Point3d(reg.X, reg.Y, 0.0));
                ed.WriteMessage("  [FAT detectado] Circulo rojo dibujado.\n");
                sesion.ToggleInOut = 0;
            }

            return true;
        }

        public static void KeywordDeshacer(Database db, Editor ed, Sesion sesion)
        {
            if (sesion.Registros.Count == 0)
            {
                ed.WriteMessage("\nNo hay registros para eliminar.\n");
                return;
            }

            int idx = sesion.Registros.Count;
            Registro last = sesion.Registros[idx - 1];
            ValoresBloque v = Atributos.Leer(db, last.Ename);
            sesion.ToggleInOut = last.ToggleInOut;

            ed.WriteMessage("- Eliminado: #" + Texto.Str(idx) +
                            " - BLQ: " + v.Codigo + v.Fat + "\n");

            Dibujo.BorrarCirculo(db, last.Circulo);
            sesion.Registros.RemoveAt(sesion.Registros.Count - 1);
            sesion.Polilinea = Dibujo.Polilinea(db, sesion.Registros, sesion.Polilinea);
        }

        public static void KeywordInOut(Database db, Editor ed, Sesion sesion)
        {
            if (sesion.Registros.Count == 0)
            {
                ed.WriteMessage("\nNo hay registros. Seleccione un bloque primero.\n");
                return;
            }

            Registro lastReg = sesion.Registros[sesion.Registros.Count - 1];
            string valFat = Atributos.GetVal(db, lastReg.Ename, Constantes.TagFat);

            if (string.IsNullOrEmpty(valFat) || valFat == " ")
            {
                ed.WriteMessage(
                    "\nEl ultimo bloque no tiene FAT. Seleccione uno con FAT primero.\n");
                return;
            }

            if (sesion.ToggleInOut == 0)
            {
                lastReg.In += 10;
                lastReg.ToggleInOut = 1;
                ed.WriteMessage("\n>> IN: +10 (Total: " + Texto.Str(lastReg.In) + ") [FAT]\n");
            }
            else
            {
                lastReg.Out += 10;
                lastReg.ToggleInOut = 0;
                ed.WriteMessage("\n>> OUT: +10 (Total: " + Texto.Str(lastReg.Out) + ") [FAT]\n");
            }
            sesion.ToggleInOut = 1 - sesion.ToggleInOut;
        }
    }
}