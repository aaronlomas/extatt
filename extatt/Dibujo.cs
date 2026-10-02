using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace extatt
{
    public static class Dibujo
    {
        public static ObjectId? Polilinea(Database db, List<Registro> dataList, ObjectId? pline)
        {
            Capas.Asegurar(db, Constantes.LayerPline);

            List<Point2d> pts = new List<Point2d>();
            foreach (Registro reg in dataList)
            {
                double px = reg.Px ?? reg.X;
                double py = reg.Py ?? reg.Y;
                pts.Add(new Point2d(px, py));
            }
            int n = pts.Count;

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
                        pl.Layer = Constantes.LayerPline;
                        pl.ColorIndex = Constantes.RedAci;
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

        public static ObjectId Circulo(Database db, Point3d pt)
        {
            Capas.Asegurar(db, Constantes.LayerCircle);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Circle circle = new Circle(new Point3d(pt.X, pt.Y, 0.0), Vector3d.ZAxis,
                                            Constantes.CircleRadius)
                {
                    Layer = Constantes.LayerCircle,
                    ColorIndex = Constantes.RedAci
                };
                BlockTableRecord space =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                space.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
                tr.Commit();
                return circle.ObjectId;
            }
        }

        public static void BorrarCirculo(Database db, ObjectId? id)
        {
            if (!id.HasValue || !id.Value.IsValid || id.Value.IsErased)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBObject obj = tr.GetObject(id.Value, OpenMode.ForWrite);
                obj.Erase();
                tr.Commit();
            }
        }
    }
}