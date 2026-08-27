export const API = import.meta.env.VITE_API_URL ?? "http://localhost:5081";

export const oturum = $state({
    token: localStorage.getItem("token") || "",
    role:"",
    currentUser: localStorage.getItem("username") || "",
    avatarUrl: localStorage.getItem("avatar_url") || "",
});

export const durum= $state({
    aktifSekme : "hareketler",
    error : "",
    loading : false,
    bildirim: "",
    secilenUrunId: null,
    bekleyenIstekSayisi: 0,
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
//
// Fiyatlandırma
//
export function fiyatKolon(fiyat) {
    if (fiyat === null || fiyat === undefined || fiyat === "") return "-";
    return (
        Number(fiyat).toLocaleString("tr-TR", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        }) + " ₺"
    );
}
//
// Sayfalama
//
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
//
// Filtreleme
//
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
//
// Sepet
//
export const sepet = $state({ satirlar: [], toplam: 0, adet: 0 });

export async function sepetYukle() {
    try {
        const res = await fetch(`${API}/api/cart`, { headers: authHeader() });
        if (!res.ok) return;
        const d = await res.json();
        sepet.satirlar = d.satirlar;
        sepet.toplam = d.toplam;
        sepet.adet = d.adet;
    } catch {

    }
}

//
// Admin: urun/kategori verisi (Urunler + Kategoriler sekmeleri ortak kullanir)
//
export async function loadAdminVeri() {
    durum.loading = true;
    durum.error = "";
    try {
        const [pRes, cRes] = await Promise.all([
            fetch(`${API}/api/products`, { headers: authHeader() }),
            fetch(`${API}/api/categories`, { headers: authHeader() }),
        ]);
        if (!pRes.ok || !cRes.ok) throw new Error("Veri alınamadı");
        veri.products = await pRes.json();
        veri.categories = await cRes.json();
    } catch (e) {
        durum.error = e instanceof Error ? e.message : String(e);
    } finally {
        durum.loading = false;
    }
}

export async function sepeteEkle(product_id, dealer_id) {
    const res = await fetch(`${API}/api/cart`, {
        method: "POST",
        headers: jsonHeader(),
        body: JSON.stringify({ product_id, dealer_id }),
    });
    if (!res.ok) throw new Error(await res.text());
    await sepetYukle();
}