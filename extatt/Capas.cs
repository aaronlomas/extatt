using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace extatt
{
    public static class Capas
    {
        public static void Asegurar(Database db, string nombre)
        {
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
                        Color = Color.FromColorIndex(ColorMethod.ByAci, Constantes.RedAci)
                    };
                    ltW.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                else
                {
                    LayerTableRecord layer =
                        (LayerTableRecord)tr.GetObject(lt[nombre], OpenMode.ForRead);
                    if (Math.Abs(layer.Color.ColorIndex) != Constantes.RedAci)
                    {
                        layer.UpgradeOpen();
                        layer.Color = Color.FromColorIndex(ColorMethod.ByAci, Constantes.RedAci);
                    }
                }
                tr.Commit();
            }
        }
    }
}