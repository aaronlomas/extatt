namespace extatt
{
    public static class Constantes
    {
        public const string LayerPline = "EXT-ATT-PLINE";
        public const string LayerCircle = "EXT-ATT-CIRCLE";

        public const string TagE = "E-01";
        public const string TagSc = "SC/H=9M";
        public const string TagElec = "01/ELEC/--";
        public const string TagNum = "NUMERACION";
        public const string TagFat = "FAT(01)";

        public const double CircleRadius = 7.0;
        public const short RedAci = 1;

        public const string CabeceraCsv =
            "No.;Codigo_Poste;Estructura;descripcion;FAT;IN;OUT;Coordenada_X;Coordenada_Y";

        public const string ArchivoCsvBase = "data_extatt_";
        public const string ArchivoCsvExt = ".csv";
    }
}