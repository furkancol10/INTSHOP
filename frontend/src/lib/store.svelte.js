export const API = import.meta.env.VITE_API_URL ?? "http://localhost:5081";

export const oturum = $state({
    token: localStorage.getItem("token") || "",
    role:"",
    currentUser: localStorage.getItem("username") || "",
    avatarUrl: localStorage.getItem("avatar_url") || "",
});

// let sp = {} 

export const durum= $state({
    aktifSekme : "hareketler",
    error : "",
    loading : false,
    bildirim: "",
});

export const veri = $state ({
    products: [],
    categories: [],
    myStock: [],
});

export function authHeader() {
    return { Authorization: oturum.token };
}

export function jsonHeader() {
    return { "Content-Type": "application/json", Authorization: oturum.token };
}

export function fiyatKolon(fiyat) {
    if (fiyat === null || fiyat === undefined || fiyat === "") return "-";
    return (
        Number(fiyat).toLocaleString("tr-TR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        }) + " ₺"
    );
}

export const pageSize = 10;
export const sayfalar = $state({});

export function sayfala(dizi, sekme) {
    const s = sayfalar[sekme] ?? 1;
    return dizi.slice((s-1) * pageSize, s * pageSize);
}

export function toplamSayfa(dizi) {
    return Math.max(1, Math.ceil(dizi.length / pageSize));
}

export function sayfaGit(sekme, yon) {
    const su = sayfalar[sekme] ?? 1;
    sayfalar[sekme] = su + yon;
}

export function sayfalariSifirla() {
  for (const k of Object.keys(sayfalar)) delete sayfalar[k];
}

export function metinAra(dizi, arama, alanlar) {
  if (!arama) return dizi;
  const q = arama.toLowerCase();
  return dizi.filter((o) =>
    alanlar.some((alan) => String(o[alan] ?? "").toLowerCase().includes(q))
  );
}

export function alanEsit(dizi, deger, alan) {
  if (!deger) return dizi;
  return dizi.filter((o) => o[alan] === deger);
}