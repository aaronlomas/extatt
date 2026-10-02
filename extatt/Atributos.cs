using Autodesk.AutoCAD.DatabaseServices;

namespace extatt
{
    public class ValoresBloque
    {
        public string E = "";
        public string Num = "";
        public string Sc = "";
        public string Elec = "";
        public string Fat = "";

        public bool SinIdentificacion
        {
            get { return E == "" && Sc == "" && Elec == "" && Num == ""; }
        }

        public string Codigo
        {
            get { return E + Num; }
        }
    }

    public static class Atributos
    {
        public static string GetVal(Database db, ObjectId ename, string tag)
        {
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                return GetVal(tr, ename, tag);
            }
        }

        public static string GetVal(Transaction tr, ObjectId ename, string tag)
        {
            if (!ename.IsValid || ename.IsErased)
                return "";

            if (!(tr.GetObject(ename, OpenMode.ForRead) is BlockReference br))
                return "";

            return Buscar(br, tr, tag);
        }

        public static ValoresBloque Leer(Database db, ObjectId ename)
        {
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                return Leer(tr, ename);
            }
        }

        public static ValoresBloque Leer(Transaction tr, ObjectId ename)
        {
            ValoresBloque v = new ValoresBloque();

            if (!ename.IsValid || ename.IsErased)
                return v;

            if (!(tr.GetObject(ename, OpenMode.ForRead) is BlockReference br))
                return v;

            v.E = Buscar(br, tr, Constantes.TagE);
            v.Num = Buscar(br, tr, Constantes.TagNum);
            v.Sc = Buscar(br, tr, Constantes.TagSc);
            v.Elec = Buscar(br, tr, Constantes.TagElec);
            v.Fat = Buscar(br, tr, Constantes.TagFat);
            return v;
        }

        private static string Buscar(BlockReference br, Transaction tr, string tag)
        {
            foreach (ObjectId attId in br.AttributeCollection)
            {
                if (!(tr.GetObject(attId, OpenMode.ForRead) is AttributeReference ar))
                    continue;
                if (string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return Texto.Str(ar.TextString);
            }
            return "";
        }
    }
}