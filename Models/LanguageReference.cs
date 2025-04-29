namespace DecodeAI.Models
{
    public static class LanguageReference
    {
        public static readonly List<string> CommonWords = new()
        {
            // Türkçe
            "bir", "ile", "çok", "gibi", "için", "ama",

            // İngilizce
            "the", "and", "this", "that", "with", "from",

            // Rusça
            "что", "как", "при", "если",

            // Fransızca
            "que", "pour", "dans", "avec",

            // Almanca
            "und", "der", "die", "ist", "nicht",

            // İspanyolca
            "que", "del", "con", "para", "como",

            // İtalyanca
            "che", "con", "per", "non",

            // Portekizce
            "que", "com", "para", "não",

            // Flemenkçe
            "het", "een", "dat", "van",

            // İsveççe
            "och", "det", "som", "med",

            // Norveççe
            "det", "som", "med", "ikke",

            // Fince
            "että", "tämä", "joka", "mikä",

            // Çince
            "的是在", // (çoklu karakterler, zaten Çince'de kelime kavramı farklıdır)

            // Japonca
            "のでに", // (Japonca kısa parçalar, aynı mantıkla)

            // Korece
            "에서이다", // (Korece parçalar)

            // Arapça
            "على", "إلى", "مع", 

            // Yunanca
            "και", "της", "από", "είναι",

            // Lehçe
            "sie", "dla", "jak", "nie",

            // Çekçe
            "když", "které", "není",

            // Romence
            "și", "în", "este", "care",

            // Hintçe
            "में", "से", "है", "और", "के"
        };
    }
}
