using System.Globalization;

namespace extatt
{
    public static class Rutas
    {
        public static string CarpetaDestino()
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
                return "";

            if (!ruta.EndsWith("\\", StringComparison.Ordinal))
                ruta += "\\";

            return ruta;
        }

        public static string ArchivoNuevo(string carpeta)
        {
            int i = 0;
            string archivo;
            do
            {
                i++;
                archivo = carpeta + Constantes.ArchivoCsvBase + (i < 10 ? "0" : "") +
                          i.ToString(CultureInfo.InvariantCulture) + Constantes.ArchivoCsvExt;
            }
            while (File.Exists(archivo));
            return archivo;
        }

        public static string ProximoCsv()
        {
            return ArchivoNuevo(CarpetaDestino());
        }
    }
}