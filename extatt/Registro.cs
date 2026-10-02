using Autodesk.AutoCAD.DatabaseServices;

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
}