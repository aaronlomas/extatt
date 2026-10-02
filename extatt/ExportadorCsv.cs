using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace extatt
{
    public static class ExportadorCsv
    {
        public static void Exportar(Database db, List<Registro> dataList, Editor ed)
        {
            if (dataList.Count == 0)
            {
                ed.WriteMessage("\n[ERROR] Seleccion vacia. No se exporto nada.\n");
                return;
            }

            string ruta = Rutas.ProximoCsv();
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
                        !Texto.KwEs(kr.StringResult, "R", "Reintentar"))
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
                fp.WriteLine(Constantes.CabeceraCsv);

                int idx = 0;
                foreach (Registro item in dataList)
                {
                    idx++;
                    ValoresBloque v = Atributos.Leer(db, item.Ename);

                    double? cx = item.X;
                    double? cy = item.Y;
                    if (v.SinIdentificacion)
                    {
                        cx = null;
                        cy = null;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(idx).Append(';');
                    sb.Append(v.E).Append(v.Num).Append(';');
                    sb.Append(v.Sc).Append(';');
                    sb.Append(v.Elec).Append(';');
                    sb.Append(v.Fat).Append(';');
                    sb.Append(Texto.Str(item.In)).Append(';');
                    sb.Append(Texto.Str(item.Out)).Append(';');
                    sb.Append(cx.HasValue ? Texto.Str(cx.Value) : "").Append(';');
                    sb.Append(cy.HasValue ? Texto.Str(cy.Value) : "");
                    fp.WriteLine(sb.ToString());
                }
            }

            ed.WriteMessage("\n================================================");
            ed.WriteMessage("\nREPORTE GENERADO CON EXITO!");
            ed.WriteMessage("\nArchivo guardado en:\n        " + ruta);
            ed.WriteMessage("\n================================================\n");
        }
    }
}