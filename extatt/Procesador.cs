using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace extatt
{
    public static class Procesador
    {
        public static Registro? Procesar(Database db, ObjectId ename, Point3d? pick)
        {
            double? px = pick?.X;
            double? py = pick?.Y;

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
    }
}