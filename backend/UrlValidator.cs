public static class UrlValidator
{
    // avatar_url / image_url gibi kullanici kontrollu resim adresleri icin allowlist:
    // yalnizca https:// ile baslayan mutlak adresler veya "/" ile baslayan (ama "//" ile
    // baslamayan - protocol-relative, disaridan bir host'a kacabilir) goreli yollar kabul
    // edilir. javascript:/data:/vbscript: gibi semalar boylece hicbir zaman veritabanina
    // yazilmaz (L-02).
    public static bool GuvenliResimUrlMi(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        url = url.Trim();

        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        if (url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
            return true;

        return false;
    }
}
