using Autodesk.AutoCAD.DatabaseServices;

namespace extatt
{
    public class Sesion
    {
        public List<Registro> Registros = new List<Registro>();
        public ObjectId? Polilinea;
        public int ToggleInOut;
    }
}