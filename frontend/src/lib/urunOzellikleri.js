// Kategoriye göre değişen ürün özellik şablonları.
// Anahtar = categories.name ile birebir eşleşmeli (ör. "Bilgisayar", "Roman").
// Eşleşme yoksa o ürün için ekstra özellik alanı gösterilmez.
export const KATEGORI_OZELLIKLERI = {
    Bilgisayar: [
        { key: "islemci", label: "İşlemci" },
        { key: "ram", label: "RAM" },
        { key: "ekran_karti", label: "Ekran Kartı" },
        { key: "depolama", label: "Depolama" },
        { key: "ekran", label: "Ekran" },
    ],
    Telefon: [
        { key: "ram", label: "RAM" },
        { key: "depolama", label: "Depolama" },
        { key: "ekran", label: "Ekran" },
        { key: "batarya", label: "Batarya" },
        { key: "kamera", label: "Kamera" },
    ],
    Roman: [
        { key: "yazar", label: "Yazar" },
        { key: "cevirmen", label: "Çevirmen" },
        { key: "yayinevi", label: "Yayınevi" },
        { key: "basim_yili", label: "Basım Yılı" },
        { key: "basim_yeri", label: "Basım Yeri" },
        { key: "sayfa_sayisi", label: "Sayfa Sayısı" },
    ],
    Gıda: [
        { key: "agirlik", label: "Ağırlık / Hacim" },
        { key: "skt", label: "Son Kullanma Tarihi" },
        { key: "icindekiler", label: "İçindekiler / Alerjenler" },
    ],
    Elektronik: [
        { key: "baglanti", label: "Bağlantı Tipi" },
        { key: "garanti", label: "Garanti Süresi" },
    ],
};

export function ozellikAlanlari(kategoriAdi) {
    return KATEGORI_OZELLIKLERI[kategoriAdi] ?? [];
}

// Backend attributes'i (jsonb) her zaman JSON metni olarak dondurur;
// bazen zaten parse edilmis obje olarak da gelebilir - ikisini de kabul et.
export function ozellikleriAyristir(raw) {
    if (!raw) return {};
    if (typeof raw === "object") return raw;
    try {
        const parsed = JSON.parse(raw);
        return parsed && typeof parsed === "object" ? parsed : {};
    } catch {
        return {};
    }
}

// Sadece verilen kategorinin alanlarina ait, bos olmayan degerleri JSON metnine cevirir.
export function ozellikleriStringify(attributes, kategoriAdi) {
    const gecerliAnahtarlar = new Set(ozellikAlanlari(kategoriAdi).map((a) => a.key));
    const temiz = {};
    for (const [k, v] of Object.entries(attributes ?? {})) {
        if (gecerliAnahtarlar.has(k) && v !== "" && v !== null && v !== undefined) {
            temiz[k] = v;
        }
    }
    return JSON.stringify(temiz);
}
