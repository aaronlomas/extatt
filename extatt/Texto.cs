using System.Globalization;

namespace extatt
{
    public static class Texto
    {
        public static string Str(string? v)
        {
            return v ?? "";
        }

        public static string Str(double v)
        {
            return Rtos(v);
        }

        public static string Str(int v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        public static string Rtos(double v)
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

        public static bool KwEs(string? resultado, string alias, string nombre)
        {
            return string.Equals(resultado, alias, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(resultado, nombre, StringComparison.OrdinalIgnoreCase);
        }
    }
}