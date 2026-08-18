<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  Chart.register(...registerables);

  const API = import.meta.env.VITE_API_URL ?? "http://localhost:5081";
  //
  // oturum
  //
  let token = $state(localStorage.getItem("token") || "");
  let role = $state("");
  let currentUser = $state(localStorage.getItem("username") || "");
  let karsilama = $state(false);
  //
  // Profil Sayfası
  //
  let avatarUrl = $state(localStorage.getItem("avatarUrl") || "");
  let profil = $state({
    username: "",
    role: "",
    address: "",
    phone: "",
    avatar_url: "",
  });
  //
  // veriler
  //
  let products = $state([]);
  let categories = $state([]);
  let error = $state("");
  let loading = $state(true);

  let name = $state("");
  let categoryId = $state("");
  let stock = $state(0);
  let price = $state(0);
  //
  //Fiyat Kolonu Düzenleme
  //
  function fiyatKolon(fiyat) {
    if (fiyat === null || fiyat === undefined || fiyat === "") {
      return "-";
    }

    return (
      Number(fiyat).toLocaleString("tr-TR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }) + "₺"
    );
  }
  //
  // Geçmiş tablosu
  //
  let miktarlar = $state({});
  let history = $state([]);
  //
  // Grafik
  //
  let chartCanvas = $state(null);
  let chartInstance = null;
  //
  //Toolbar
  //
  let aktifSekme = $state("hareketler");
  let kategoriListesi = $derived(
    categories.map((c) => ({
      id: c.id,
      etiket: c.parent_id ? `\u00A0\u00A0- ${c.name}` : c.name,
    })),
  );
  //
  // Giriş
  //
  let loginUser = $state("");
  let loginPass = $state("");
  let loginError = $state("");
  //
  //Kayıt Olma
  //
  let kayitModu = $state(false);
  let kayitForm = $state({
    username: "",
    password: "",
    email: "",
    address: "",
    phone: "",
  });
  let kayitMesaj = $state("");
  //
  //Kullanıcı ekleme
  //
  let modalAcik = $state(false);
  let yeniKullanici = $state({
    username: "",
    password: "",
    role: "",
    address: "",
    phone: "",
  });
  //
  //Ürün Ekleme
  //
  let urunModalAcik = $state(false);
  let duzenlenenId = $state(false);
  let urunForm = $state({
    name: "",
    category_id: "",
    price: "",
    image_url: "",
  });
  //
  //Kategori Ekleme
  //
  let kategoriModalAcik = $state(false);
  let acikKategori = $state(null);
  let yeniAltAd = $state("");
  let altHata = $state("");

  let anaKategoriler = $derived(categories.filter((c) => !c.parent_id));
  //
  //Resim Önizleme
  //
  let onizlemeYolu = $derived.by(() => {
    let v = urunForm.image_url?.trim() || "";
    if (!v) return "";
    if (v.startsWith("/") || v.startsWith("http")) return v;
    if (!/\.(jpg|jpeg|png|webp|gif)$/i.test(v)) v += ".jpg";
    return `/products/${v}`;
  });
  //
  //bayi oturumu
  //
  let myStock = $state([]);
  let dealers = $state([]);
  let myMovements = $state([]);
  //
  //admin oturumu
  //
  let movements = $state([]);
  let requests = $state([]);
  let requestsFiltre = $state("all");
  let bekleyenSayi = $derived(
    requests.filter((r) => r.status === "pending").length,
  );
  //
  //Kullanıcı oturumu
  //
  let users = $state([]);
  //
  //Mağaza
  //
  let shopData = $state([]);
  let shopOffset = $state([]);
  let hepsiYuklendi = $state(false);
  let yukleniyorShop = $state(false);
  const shopLimit = 14;
  let sentinel = $state(null);
  //
  //Bayi Stok-fiyat
  //
  let islemModalAcik = $state(false);
  let secilenUrunId = $state("");
  let islemTuru = $state("");
  let hareketMiktar = $state("");
  let yeniFiyat = $state("");
  let modalHata = $state("");
  let bildirim = $state("");

  let secilenUrun = $derived(
    myStock.find((p) => p.product_id === Number(secilenUrunId)) ?? null,
  );

  let fiyatDurum = $derived.by(() => {
    if (islemTuru !== "fiyat" || !secilenUrun || !yeniFiyat) return null;
    const f = Number(yeniFiyat);
    if (isNaN(f)) return null;
    const alt = Number(secilenUrun.alt_sinir);
    const ust = Number(secilenUrun.ust_sinir);
    if (f < alt || f > ust) return "disarida";
    const aralik = ust - alt;
    if (f - alt < aralik * 0.15 || ust - f < aralik * 0.15) return "sinirda";
    return "iyi";
  });
  //
  //Depo Durum
  //
  let maxStok = $derived(
    Math.max(...myStock.map((p) => Number(p.stock) || 0), 1),
  );

  function depoDurumu(stok) {
    const s = Number(stok) || 0;
    const yuzde = Math.min(100, Math.round((s / 100) * 100));
    let sinif = "bol";
    if (s === 0) sinif = "bos";
    else if (s <= 10) sinif = "kritik";
    else if (s <= 30) sinif = "az";
    else if (s <= 60) sinif = "normal";
    return { yuzde, sinif, sayi: s };
  }
  //
  //Sayfalama
  //
  const pageSize = 10;
  let sayfalar = $state({});

  function sayfala(dizi, sekme) {
    const s = sayfalar[sekme] ?? 1;
    return dizi.slice((s - 1) * pageSize, s * pageSize);
  }

  function toplamSayfa(dizi) {
    return Math.max(1, Math.ceil(dizi.length / pageSize));
  }

  function sayfaGit(sekme, yon) {
    const su = sayfalar[sekme] ?? 1;
    sayfalar[sekme] = su + yon;
  }
  //
  // Giriş ekranı
  //
  async function login() {
    loginError = "";
    try {
      const res = await fetch(`${API}/api/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: loginUser, password: loginPass }),
      });
      if (!res.ok) throw new Error("Kullanıcı adı veya şifre hatalı");
      const data = await res.json();
      token = data.token;
      role = data.role;
      currentUser = data.username;
      avatarUrl = data.avatar_url || "";
      localStorage.setItem("token", token);
      localStorage.setItem("username", currentUser);
      localStorage.setItem("avatar_url", avatarUrl);
      if (role === "Bayi") {
        aktifSekme = "anasayfa";
        await loadMyStock();
        await loadMyMovements();
        await loadHistory();
      } else if (role == "Admin") {
        aktifSekme = "hareketler";
        await loadAll();
        await loadMovements();
        await loadDealers();
        await loadUsers();
      } else if (role == "Kullanici") {
        aktifSekme = "magaza";
        shopSifirla();
        await loadShop();
      }
      karsilama = true;
      setTimeout(() => {
        karsilama = false;
      }, 2000);
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    }
  }

  function logout() {
    token = "";
    role = "";
    currentUser = "";
    avatarUrl = "";
    aktifSekme = "anasayfa";
    products = [];
    movements = [];
    myStock = [];
    history = [];
    dealers = [];
    users = [];
    sayfalar = {};
    localStorage.clear();
    location.reload();
    shopSifirla();
  }
  //
  //Kayıt Ol
  //
  async function kayitOl() {
    kayitMesaj = "";
    loginError = "";
    try {
      const res = await fetch(`${API}/api/signup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(kayitForm),
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || `HTTP ${res.status}`);
      }
      kayitMesaj = "Kayıt başarılı ! Şimdi giriş yapabilirsiniz.";
      loginUser = kayitForm.username;
      kayitForm = {
        username: "",
        password: "",
        email: "",
        address: "",
        phone: "",
      };
      kayitModu = false;
    } catch (e) {
      loginError = e instanceof Error ? e.message : string(e);
    }
  }

  let epostaGecerli = $derived.by(() => {
    const e = kayitForm.email?.trim() || "";
    if (!e) return null; // boşsa henüz bir şey söyleme
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e);
  });
  //
  // Sayfa Fonksiyonları
  //
  async function loadAll() {
    loading = true;
    error = "";
    try {
      const [pRes, cRes] = await Promise.all([
        fetch(`${API}/api/products`),
        fetch(`${API}/api/categories`),
      ]);
      if (!pRes.ok || !cRes.ok) throw new Error("Veri alınamadı");
      products = await pRes.json();
      categories = await cRes.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    } finally {
      loading = false;
    }
  }

  async function loadDealers() {
    try {
      const res = await fetch(`${API}/api/dealers`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      dealers = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadUsers() {
    try {
      const res = await fetch(`${API}/api/users`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      users = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadShop() {
    console.log(
      "loadShop çağrıldı — offset:",
      shopOffset,
      "hepsiYuklendi:",
      hepsiYuklendi,
      "yukleniyor:",
      yukleniyorShop,
    );
    if (yukleniyorShop || hepsiYuklendi) {
      console.log("→ ÇIKIŞ: kilit veya veri bitti");
      return;
    }
    yukleniyorShop = true;
    try {
      const res = await fetch(
        `${API}/api/shop?offset=${shopOffset}&limit=${shopLimit}`,
        {
          headers: { Authorization: token },
        },
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const yeni = await res.json();

      if (yeni.length < shopLimit) hepsiYuklendi = true;

      shopData = [...shopData, ...yeni];
      shopOffset += yeni.length;
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    } finally {
      loading = false;
      yukleniyorShop = false;
    }
  }

  function shopSifirla() {
    shopData = [];
    shopOffset = 0;
    hepsiYuklendi = false;
  }

  $effect(() => {
    if (aktifSekme !== "magaza" || !sentinel) return;

    const gozlemci = new IntersectionObserver(
      (girisler) => {
        if (girisler[0].isIntersecting) {
          loadShop();
        }
      },
      { rootMargin: "200px" },
    );

    gozlemci.observe(sentinel);

    return () => gozlemci.disconnect();
  });

  async function loadProfile() {
    try {
      const res = await fetch(`${API}/api/profile`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      profil = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function profilGuncelle() {
    try {
      const res = await fetch(`${API}/api/profile`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({
          address: profil.address,
          phone: profil.phone,
          avatar_url: profil.avatar_url,
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      avatarUrl = profil.avatar_url || "";
      localStorage.setItem("avatar_url", avatarUrl);
      alert("Profil Güncellendi");
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  $effect(() => {
    if (aktifSekme === "profil") {
      loadProfile();
    }
  });
  //
  //Bayi Stok-Fiyat
  //
  function islemModalAc(productId = "") {
    secilenUrunId = productId;
    islemTuru = "";
    hareketMiktar = "";
    yeniFiyat = "";
    modalHata = "";
    islemModalAcik = true;
  }

  async function islemKaydet() {
    modalHata = "";

    if (!secilenUrunId) {
      modalHata = "Ürün seçin";
      return;
    }
    if (!islemTuru) {
      modalHata = "İşlem türü seçin";
      return;
    }

    const pid = Number(secilenUrunId);

    if (islemTuru === "fiyat") {
      const f = Number(yeniFiyat);
      if (!yeniFiyat || isNaN(f) || f <= 0) {
        modalHata = "Geçerli bir fiyat girin";
        return;
      }
      if (fiyatDurum === "disarida") {
        modalHata = `Fiyat ${fiyatKolon(secilenUrun.alt_sinir)} - ${fiyatKolon(secilenUrun.ust_sinir)} aralığında olmalı`;
        return;
      }
      try {
        const res = await fetch(`${API}/api/my-stock/price`, {
          method: "PUT",
          headers: { "Content-Type": "application/json", Authorization: token },
          body: JSON.stringify({ product_id: pid, price: f }),
        });
        if (!res.ok)
          throw new Error((await res.text()) || `HTTP ${res.status}`);
        islemModalAcik = false;
        bildirim = "Fiyat talebiniz onaya gönderildi.";
        setTimeout(() => (bildirim = ""), 4000);
        await loadMyStock();
        await loadMyRequests();
      } catch (e) {
        modalHata = e instanceof Error ? e.message : String(e);
      }
    } else {
      const m = Math.abs(Number(hareketMiktar) || 0);
      if (m <= 0) {
        modalHata = "Miktar 0'dan büyük olmalı";
        return;
      }
      const degisim = islemTuru === "giris" ? m : -m;
      try {
        const res = await fetch(`${API}/api/my-stock/movement`, {
          method: "POST",
          headers: { "Content-Type": "application/json", Authorization: token },
          body: JSON.stringify({ product_id: pid, change: degisim }),
        });
        if (!res.ok)
          throw new Error((await res.text()) || `HTTP ${res.status}`);
        islemModalAcik = false;
        await loadMyStock();
        await loadMyMovements();
        await loadHistory();
      } catch (e) {
        modalHata = e instanceof Error ? e.message : String(e);
      }
    }
  }
  //
  // Kategori sayfası
  //
  function altlariGetir(anaId) {
    return categories.filter((c) => c.parent_id === anaId);
  }

  function kategoriDetayAc(kategori) {
    acikKategori = kategori;
    yeniAltAd = "";
    altHata = "";
    kategoriDetayAc = true;
  }
  async function altKategoriEkle() {
    altHata = "";
    if (!yeniAltAd.trim()) {
      altHata = "Kategori adı zorunlu";
      return;
    }
    try {
      const res = await fetch(`${API}/api/categories`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({
          name: yeniAltAd.trim(),
          parent_id: acikKategori.id,
        }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      yeniAltAd = "";
      await loadAll();
    } catch (e) {
      altHata = e instanceof Error ? e.message : String(e);
    }
  }

  async function altKategoriSil(id) {
    altHata = "";
    try {
      const res = await fetch(`${API}/api/categories/${id}`, {
        method: "Delete",
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      await loadAll();
    } catch (e) {
      altHata = e instanceof Error ? e.message : String(e);
    }
  }
  //
  // Ürünlerin Fonksiyonları
  //
  function urunEkleAc() {
    duzenlenenId = null;
    urunForm = { name: "", category_id: "", price: "", image_url: "" };
    urunModalAcik = true;
  }

  function urunDuzenleAc(p) {
    duzenlenenId = p.id;
    urunForm = {
      name: p.name,
      category_id: p.category_id ?? "",
      price: p.price,
      image_url: p.image_url ?? "",
    };
    urunModalAcik = true;
  }

  async function urunKaydet() {
    const url = duzenlenenId
      ? `${API}/api/products/${duzenlenenId}`
      : `${API}/api/products`;
    const method = duzenlenenId ? "PUT" : "POST";

    let resimYolu = urunForm.image_url?.trim() || "";
    if (
      resimYolu &&
      !resimYolu.startsWith("/") &&
      !resimYolu.startsWith("http")
    ) {
      if (!/\.(jpg|jpeg|png|webp|gif)$/i.test(resimYolu)) {
        resimYolu += ".jpg";
      }
      resimYolu = `/products/${resimYolu}`;
    }

    try {
      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({
          name: urunForm.name,
          category_id: Number(urunForm.category_id),
          price: Number(urunForm.price),
          image_url: resimYolu,
        }),
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || `HTTP ${res.status}`);
      }
      urunModalAcik = false;
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function addProduct() {
    if (!name || !categoryId) {
      error = "Ad ve kategori zorunlu";
      return;
    }
    try {
      const res = await fetch(`${API}/api/products`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: token,
        },
        body: JSON.stringify({
          name,
          category_id: Number(categoryId),
          stock: Number(stock),
          price: Number(price),
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      name = "";
      categoryId = "";
      stock = 0;
      price = 0;
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function deleteProduct(id) {
    try {
      const res = await fetch(`${API}/api/products/${id}`, {
        method: "DELETE",
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }
  //
  // Admin onay fonksiyonu
  //
  async function laodRequests() {
    try {
      const res = await fetch(`${API}/api/requests?status=${requestsFiltre}`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      requests = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function talepKarar(id, karar, not = "") {
    try {
      const res = await fetch(`${API}/api/requests/${id}/${karar}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({ note: not }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      await laodRequests();
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  $effect(() => {
    if (role === "Admin" && aktifSekme === "istekler") {
      laodRequests();
    }
  });
  //
  //Bayi Fonksiyonları
  //
  async function loadMyStock() {
    try {
      const res = await fetch(`${API}/api/my-stock`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      myStock = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    } finally {
      loading = false;
    }
  }

  async function loadMyMovements() {
    try {
      const res = await fetch(`${API}/api/my-stock/movements`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      myMovements = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadMovements() {
    try {
      const res = await fetch(`${API}/api/movements`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      movements = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function movement(productId, change) {
    try {
      const res = await fetch(`${API}/api/my-stock/movement`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({ product_id: productId, change }),
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || `HTTP ${res.status}`);
      }
      miktarlar[productId] = "";
      await loadMyStock();
      await loadHistory();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadHistory() {
    try {
      const res = await fetch(`${API}/api/my-stock/history`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      history = await res.json();
      setTimeout(drawChart, 0);
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }
  //
  // Grafik Fonksiyonu
  //
  function drawChart() {
    if (!chartCanvas || !history.length) return;

    if (chartInstance) chartInstance.destroy();

    chartInstance = new Chart(chartCanvas, {
      type: "bar",
      data: {
        labels: history.map((h) =>
          new Date(h.tarih).toLocaleDateString("tr-TR"),
        ),
        datasets: [
          {
            label: "Giriş",
            data: history.map((h) => h.giris),
            backgroundColor: "#22a722",
          },
          {
            label: "Çıkış",
            data: history.map((h) => h.cikis),
            backgroundColor: "#c00",
          },
        ],
      },
      options: {
        responsive: true,
        scales: { y: { beginAtZero: true } },
      },
    });
  }

  $effect(() => {
    if (aktifSekme === "raporlar" && history.length) {
      setTimeout(drawChart, 0);
    }
  });
  //
  //Kullanıcı Fonksiyonu
  //
  async function kullaniciekle() {
    try {
      const res = await fetch(`${API}/api/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify(yeniKullanici),
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || `HTTP ${res.status}`);
      }
      yeniKullanici = {
        username: "",
        password: "",
        role: "Kullanici",
        address: "",
        phone: "",
      };
      modalAcik = false;
      await loadUsers();
      await loadDealers();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(async () => {
    if (!token) return;

    try {
      const res = await fetch(`${API}/api/profile`, {
        headers: { Authorization: token },
      });
      if (!res.ok) {
        logout();
        return;
      }
      const p = await res.json();
      role = p.role;
      currentUser = p.username;
      avatarUrl = p.avatarUrl || "";
      localStorage.setItem("username", currentUser);
      localStorage.setItem("avatar_url", avatarUrl);
    } catch {
      logout();
      return;
    }
    if (role === "Bayi") {
      aktifSekme = "anasayfa";
      loadMyStock();
      loadMyMovements();
      loadHistory();
    } else if (role === "Admin") {
      aktifSekme = "hareketler";
      loadAll();
      loadMovements();
      loadDealers();
      loadUsers();
    } else if (role === "Kullanici") {
      aktifSekme = "magaza";
      shopSifirla();
      loadShop();
    }
  });
</script>

<main>
  {#if !token}
    <div class="login-wrapper">
      <div class="flip-cerceve">
        <div class="flip-ic" class:donuk={kayitModu}>
          <div class="flip-yuz flip-on" inert={kayitModu}>
            <div class="login-card">
              <div class="avatar">
                <svg viewBox="0 0 24 24" width="40" height="40" fill="white">
                  <path
                    d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"
                  />
                </svg>
              </div>

              <div class="input-group">
                <span class="input-icon">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="#888"
                    ><path
                      d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"
                    /></svg
                  >
                </span>
                <input placeholder="Kullanıcı Adı" bind:value={loginUser} />
              </div>

              <div class="input-group">
                <span class="input-icon">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="#888"
                    ><path
                      d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3-9H9V6c0-1.66 1.34-3 3-3s3 1.34 3 3v2z"
                    /></svg
                  >
                </span>
                <input
                  type="password"
                  placeholder="Şifre"
                  bind:value={loginPass}
                  onkeydown={(e) => e.key === "Enter" && login()}
                />
              </div>

              <button class="login-btn" onclick={login}>Giriş</button>
              <button
                class="mod-degistir"
                onclick={() => {
                  kayitModu = true;
                  (loginError = ""), (kayitMesaj = "");
                }}
              >
                Hesabın yok mu? Kayıt Ol
              </button>
              {#if kayitMesaj}<p class="basari">{kayitMesaj}</p>{/if}
              {#if loginError && !kayitModu}<p class="error">
                  {loginError}
                </p>{/if}
            </div>
          </div>

          <div class="flip-yuz flip-arka" inert={!kayitModu}>
            <div class="login-card">
              <h3 class="kayit-baslik">Kayıt Ol</h3>
              <div class="input-group">
                <input
                  placeholder="Kullanıcı Adı"
                  bind:value={kayitForm.username}
                />
              </div>
              <div class="input-group">
                <input
                  type="password"
                  placeholder="Şifre (en az 6 karakter)"
                  bind:value={kayitForm.password}
                />
              </div>
              <div class="input-group" class:hatali={epostaGecerli === false}>
                <input
                  type="email"
                  placeholder="E-Posta"
                  bind:value={kayitForm.email}
                />
              </div>
              {#if epostaGecerli === false}
                <p class="ipucu-hata">
                  Geçerli bir E-Posta giriniz. (ornek@site.com)
                </p>
              {/if}
              <div class="input-group">
                <input placeholder="Adres" bind:value={kayitForm.address} />
              </div>
              <div class="input-group">
                <input placeholder="Telefon" bind:value={kayitForm.phone} />
              </div>
              <button class="login-btn" onclick={kayitOl}>Kayıt Ol</button>
              <button
                class="mod-degistir"
                onclick={() => {
                  kayitModu = false;
                  loginError = "";
                }}
              >
                Zaten hesabım var -Giriş Yap
              </button>
              {#if loginError && kayitModu}<p class="error">
                  {loginError}
                </p>{/if}
            </div>
          </div>
        </div>
        <!-- Ic -->
      </div>
      <!-- Cerceve -->
    </div>
    <!-- Wrapper -->
  {:else if karsilama}
    <div class="karsilama-ekran">
      {#if avatarUrl}
        <img src={avatarUrl} alt="avatar" class="karsilama-avatar" />
      {/if}
      <h1>Hoş Geldiniz, {currentUser}!</h1>
      <div class="spinner"></div>
      <p>Sayfa Yükleniyor...</p>
    </div>
  {:else}
    <div class="toolbar">
      <div class="toolbar-ic">
        <button
          class="toolbar-baslik"
          onclick={() =>
            (aktifSekme = role === "Admin" ? "hareketler" : "anasayfa")}
          >KOBURA</button
        >

        {#if role === "Admin"}
          <button
            class:aktif={aktifSekme === "dealers"}
            onclick={() => (aktifSekme = "dealers")}>Bayiler</button
          >
          <button
            class:aktif={aktifSekme === "urunler"}
            onclick={() => (aktifSekme = "urunler")}>Ürünler</button
          >
          <button
            class:aktif={aktifSekme === "kategoriler"}
            onclick={() => (aktifSekme = "kategoriler")}>Kategoriler</button
          >
          <button
            class:aktif={aktifSekme === "istekler"}
            onclick={() => (aktifSekme = "istekler")}
            >İstekler {#if bekleyenSayi > 0}<span class="badge"
                >{bekleyenSayi}</span
              >{/if}</button
          >
          <button
            class:aktif={aktifSekme === "users"}
            onclick={() => (aktifSekme = "users")}>Kullanıcılar</button
          >
        {:else if role === "Bayi"}
          <button
            class:aktif={aktifSekme === "stok"}
            onclick={() => (aktifSekme = "stok")}>Stok</button
          >
          <button
            class:aktif={aktifSekme === "raporlar"}
            onclick={() => (aktifSekme = "raporlar")}>Raporlar</button
          >
          <button
            class:aktif={aktifSekme === "fiyatlandirma"}
            onclick={() => (aktifSekme = "fiyatlandirma")}>Fiyatlandırma</button
          >
        {:else if role === "Kullanici"}
          <button
            class:aktif={aktifSekme === "magaza"}
            onclick={() => (aktifSekme = "magaza")}>Mağaza</button
          >
          <button
            class:aktif={aktifSekme === "indirim"}
            onclick={() => (aktifSekme = "indirim")}>İndirimler</button
          >
        {/if}

        <span class="toolbar-spacer"></span>
        <button class="profil-btn" onclick={() => (aktifSekme = "profil")}>
          {#if avatarUrl}
            <img src={avatarUrl} alt="avatar" class="toolbar-avatar" />
          {/if}
          <span>{currentUser}</span>
        </button>
        <button class="cikis-btn" onclick={logout}>Çıkış</button>
      </div>
    </div>

    {#if bildirim}
      <div class="bildirim">{bildirim}</div>
    {/if}

    <div class="sekme-icerik">
      {#if aktifSekme === "anasayfa"}
        <div class="hosgeldin">
          <h1>Hoş Geldiniz, {currentUser}!</h1>
          <p>
            Stok işlemleriniz için "Stok", raporlarınız için "Raporlar"
            sekmesini kullanın.
          </p>
        </div>
      {:else if aktifSekme === "stok"}
        <h2>Stok Yönetimi</h2>
        <div class="tablo-cerceve">
          <button class="islem-btn1" onclick={() => islemModalAc("")}
            >Stok/fiyat</button
          >
          <!-- Stok Tablosu -->
          <table>
            <thead>
              <tr
                ><th>Ürün-Id</th><th>Ürün</th><th>Bayi Fiyatı</th><th
                  >Kategori</th
                ><th>Depo Durumu</th><th>Son Güncelleme Tarihi</th><th>İşlem</th
                ></tr
              >
            </thead>
            <tbody>
              {#each myStock as p}
                {@const d = depoDurumu(p.stock)}
                <tr class:dusuk={p.stock < 10}>
                  <td>{p.product_id}</td>
                  <td>{p.name}</td>
                  <td>{p.benim_fiyatim ?? "-"}</td>
                  <td>{p.category}</td>
                  <td>
                    <div class="depo-bar">
                      <div
                        class="depo-dolu {d.sinif}"
                        style="width: {d.yuzde}%"
                      ></div>
                      <span class="depo-yazi">{d.sayi}</span>
                    </div>
                    <span class="depo-etiket {d.sinif}">{d.etiket}</span>
                  </td>
                  <td class="tarih-hucre"
                    >{p.son_hareket
                      ? new Date(p.son_hareket).toLocaleString("tr-TR")
                      : "-"}</td
                  >
                  <td
                    ><button
                      class="islem-btn"
                      onclick={() => islemModalAc(p.product_id)}
                      >Stok/fiyat</button
                    ></td
                  >
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {:else if aktifSekme === "raporlar"}
        <h2>Raporlar</h2>
        <div style="display: flex-wrap; min-width: 280px">
          <h3>Giriş / Çıkış Geçmişi</h3>
          {#if myMovements.length}
            <div class="tablo-cerceve">
              <table>
                <thead>
                  <tr>
                    <th>Tarih</th><th>Ürün</th><th>İşlem</th><th>Miktar</th>
                  </tr>
                </thead>
                <tbody>
                  {#each sayfala(myMovements, "raporlar") as m}
                    <tr>
                      <td
                        >{new Date(m.created_at).toLocaleDateString(
                          "tr-TR",
                        )}</td
                      >
                      <td>{m.urun}</td>
                      <td style="color: {m.quantity > 0 ? 'green' : 'red'}"
                        >{m.quantity > 0 ? "Giriş" : "Çıkış"}</td
                      >
                      <td>{Math.abs(m.quantity)}</td>
                    </tr>
                  {/each}
                </tbody>
              </table>
              <div class="pagination">
                <button
                  onclick={() => sayfaGit("raporlar", -1)}
                  disabled={(sayfalar.raporlar ?? 1) === 1}>Önceki</button
                >
                <span
                  >Sayfa {sayfalar.raporlar ?? 1} / {toplamSayfa(
                    myMovements,
                  )}</span
                >
                <button
                  onclick={() => sayfaGit("raporlar", 1)}
                  disabled={(sayfalar.raporlar ?? 1) ===
                    toplamSayfa(myMovements)}>Sonraki</button
                >
              </div>
            </div>
          {:else}
            <p>Henüz giriş/çıkış yapılmamış.</p>
          {/if}
        </div>

        <div style="display: flex-wrap; min-width: 350px;">
          <h3>Giriş / Çıkış Grafiği</h3>
          {#if history.length}
            <div style="max-width: 600px;">
              <canvas bind:this={chartCanvas}></canvas>
            </div>
          {/if}
        </div>
      {:else if aktifSekme === "kategoriler"}
        <div class="sekme-baslik">
          <h2>Kategoriler</h2>
          <button
            class="ekle-btn"
            onclick={() => {
              kategoriModalAcik = true;
              kategoriHata = "";
            }}>+ Yeni Ana Kategori</button
          >
        </div>

        {#if anaKategoriler.length}
          <div class="kategori-kartlari">
            {#each anaKategoriler as ana}
              {@const altlar = altlariGetir(ana.id)}
              <button
                class="kategori-kart"
                onclick={() => kategoriDetayAc(ana)}
              >
                <div class="kart-ust">
                  <h3>{ana.name}</h3>
                  <span class="kart-sayi">{altlar.length}</span>
                </div>

                <div class="kart-altlar">
                  {#if altlar.length}
                    {#each altlar.slice(0, 4) as alt}
                      <span class="alt-etiket">{alt.name}</span>
                    {/each}
                    {#if altlar.length > 4}
                      <span class="alt-etiket daha"
                        >+{altlar.length - 4} daha</span
                      >
                    {/if}
                  {:else}
                    <span class="bos-yazi">Alt Kategori Yok</span>
                  {/if}
                </div>
              </button>
            {/each}
          </div>
        {:else}
          <p>Kategori Yok</p>
        {/if}
      {:else if aktifSekme === "istekler"}
        <h2>Bekleyen İstekler</h2>
        <div class="filtre-satir">
          <button
            class:aktif={requestsFiltre === "all"}
            onclick={() => (requestsFiltre = "all")}>Hepsi</button
          >
          <button
            class:aktif={requestsFiltre === "pending"}
            onclick={() => (requestsFiltre = "pending")}>Bekleyen</button
          >
          <button
            class:aktif={requestsFiltre === "approved"}
            onclick={() => (requestsFiltre = "approved")}>Onaylanan</button
          >
          <button
            class:aktif={requestsFiltre === "rejected"}
            onclick={() => (requestsFiltre = "rejected")}>Reddedilen</button
          >
        </div>

        {#if requests.length}
          <div class="tablo-cerceve">
            <table>
              <thead>
                <tr>
                  <th>Bayi</th><th>Ürün</th><th>Eski Fiyat</th><th
                    >Yeni Fiyat</th
                  ><th>Aralık</th><th>Tarih</th><th>Durum</th><th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {#each requests as r}
                  <tr>
                    <td>{r.bayi}</td>
                    <td>{r.urun}</td>
                    <td>{fiyatKolon(r.old_price)}</td>
                    <td><strong>{fiyatKolon(r.new_price)}</strong></td>
                    <td class="kucuk"
                      >{fiyatKolon(r.alt_sinir)} - {fiyatKolon(r.ust_sinir)}</td
                    >
                    <td class="kucuk"
                      >{new Date(r.created_at).toLocaleString("tr-TR")}</td
                    >
                    <td>
                      {#if r.status === "pending"}
                        <span class="durum bekliyor">Bekliyor</span>
                      {:else if r.status === "approved"}
                        <span class="durum onayli">Onaylandı</span>
                      {:else if r.status === "rejected"}
                        <span class="durum redli">Reddedildi</span>
                      {:else}
                        <span class="durumlar">{r.status}</span>
                      {/if}
                      {#if r.admin_note}<div class="kucuk">
                          {r.admin_note}
                        </div>{/if}
                    </td>
                    <td>
                      {#if r.status === "pending"}
                        <button
                          class="onay-btn"
                          onclick={() => talepKarar(r.id, "approve")}
                          >Onayla</button
                        >
                        <button
                          class="red-btn"
                          onclick={() =>
                            talepKarar(
                              r.id,
                              "reject",
                              (prompt = "Red sebebi:"),
                            ) ?? ""}>Reddet</button
                        >
                      {:else}
                        <span class="kucuk">-</span>
                      {/if}
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
            <div class="pagination">
              <button
                onclick={() => sayfaGit("istekler", -1)}
                disabled={(sayfalar.istekler ?? 1) === 1}>Önceki</button
              >
              <span
                >Sayfa {sayfalar.istekler ?? 1} / {toplamSayfa(requests)}</span
              >
              <button
                onclick={() => sayfaGit("istekler", 1)}
                disabled={(sayfalar.istekler ?? 1) === toplamSayfa(requests)}
                >Sonraki</button
              >
            </div>
          </div>
        {:else}
          <p>Bu durumda istek yok.</p>
        {/if}
      {:else if aktifSekme === "hareketler"}
        <h2>Bayi hareketleri</h2>
        {#if movements.length}
          <div class="tablo-cerceve">
            <table>
              <thead>
                <tr
                  ><th>Bayi</th><th>Ürün</th><th>İşlem</th><th>Miktar</th><th
                    >Tarih</th
                  ></tr
                >
              </thead>
              <tbody>
                {#each sayfala(movements, "hareketler") as m}
                  <tr>
                    <td>{m.bayi}</td>
                    <td>{m.urun}</td>
                    <td style="color: {m.quantity > 0 ? 'green' : '#c00'}">
                      {m.quantity > 0 ? "Giriş" : "Çıkış"}
                    </td>
                    <td>{Math.abs(m.quantity)}</td>
                    <td>{new Date(m.created_at).toLocaleString("tr-TR")}</td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>
          <div class="pagination">
            <button
              onclick={() => sayfaGit("hareketler", -1)}
              disabled={(sayfalar.hareketler ?? 1) === 1}>Önceki</button
            >
            <span
              >Sayfa {sayfalar.hareketler ?? 1} / {toplamSayfa(movements)}</span
            >
            <button
              onclick={() => sayfaGit("hareketler", 1)}
              disabled={(sayfalar.hareketler ?? 1) === toplamSayfa(movements)}
              >Sonraki</button
            >
          </div>
        {:else}
          <p>Henüz hareket yok.</p>
        {/if}
      {:else if aktifSekme === "urunler"}
        <h2>Ürünler</h2>
        <button class="ekle-btn" onclick={urunEkleAc}>+ Yeni Ürün</button>

        {#if products.length}
          <div class="tablo-cerceve">
            <table>
              <thead>
                <tr>
                  <th>ID</th><th>Resim</th><th>Ürün</th><th>Kategori</th><th
                    >Stok</th
                  ><th>Fiyat</th><th></th>
                </tr>
              </thead>
              <tbody>
                {#each sayfala(products, "urunler") as p}
                  <tr class:dusuk={p.stock < 10}>
                    <td>{p.id}</td>
                    <td>
                      {#if p.image_url}
                        <img
                          src={p.image_url}
                          alt={p.name}
                          style="width:40px; height: 40px; object-fit: cover; border-radius: 4px;"
                        />
                      {/if}
                    </td>
                    <td>{p.name}</td>
                    <td
                      >{p.parent_category
                        ? `${p.parent_category} › ${p.category}`
                        : p.category}</td
                    >
                    <td>{p.stock}</td>
                    <td>{fiyatKolon(p.price)}</td>
                    <td>
                      <button
                        class="duzenle-btn"
                        onclick={() => urunDuzenleAc(p)}>Düzenle</button
                      >
                      <button class="sil" onclick={() => deleteProduct(p.id)}
                        >Sil</button
                      ></td
                    >
                  </tr>
                {/each}
              </tbody>
            </table>
            <div class="pagination">
              <button
                onclick={() => sayfaGit("urunler", -1)}
                disabled={(sayfalar.urunler ?? 1) === 1}>Önceki</button
              >
              <span
                >Sayfa {sayfalar.urunler ?? 1} / {toplamSayfa(products)}</span
              >
              <button
                onclick={() => sayfaGit("urunler", 1)}
                disabled={(sayfalar.urunler ?? 1) === toplamSayfa(products)}
                >Sonraki</button
              >
            </div>
          </div>
        {/if}
      {:else if aktifSekme === "dealers"}
        <h2>Bayiler</h2>
        {#if dealers.length}
          <div class="tablo-cerceve">
            <table>
              <thead>
                <tr
                  ><th>ID</th><th>Bayi Adı</th><th>Adres</th><th>Telefon</th
                  ></tr
                >
              </thead>
              <tbody>
                {#each sayfala(dealers, "dealers") as d}
                  <tr>
                    <td>{d.id}</td>
                    <td>{d.username}</td>
                    <td>{d.address ?? "-"}</td>
                    <td>{d.phone ?? "-"}</td>
                  </tr>
                {/each}
              </tbody>
            </table>
            <div class="pagination">
              <button
                onclick={() => sayfaGit("dealers", -1)}
                disabled={(sayfalar.dealers ?? 1) === 1}>Önceki</button
              >
              <span>Sayfa {sayfalar.dealers ?? 1} / {toplamSayfa(dealers)}</span
              >
              <button
                onclick={() => sayfaGit("dealers", 1)}
                disabled={(sayfalar.dealers ?? 1) === toplamSayfa(dealers)}
                >Sonraki</button
              >
            </div>
          </div>
        {:else}
          <p>Henüz bayi yok.</p>
        {/if}
      {:else if aktifSekme === "users"}
        <h2>Kullanıcılar</h2>
        <button class="ekle-btn" onclick={() => (modalAcik = true)}>
          + Yeni Kullanıcı</button
        >

        {#if users.length}
          <div class="tablo-cerceve">
            <table>
              <thead>
                <tr><th>ID</th><th>Kullanıcı</th><th>Rol</th></tr>
              </thead>
              <tbody>
                {#each sayfala(users, "users") as u}
                  <tr>
                    <td>{u.id}</td>
                    <td>{u.username}</td>
                    <td>{u.role}</td>
                  </tr>
                {/each}
              </tbody>
            </table>
            <div class="pagination">
              <button
                onclick={() => sayfaGit("users", -1)}
                disabled={(sayfalar.users ?? 1) === 1}>Önceki</button
              >
              <span>Sayfa {sayfalar.users ?? 1} / {toplamSayfa(users)}</span>
              <button
                onclick={() => sayfaGit("users", 1)}
                disabled={(sayfalar.users ?? 1) === toplamSayfa(users)}
                >Sonraki</button
              >
            </div>
          </div>
        {:else}
          <p>Henüz kullanıcı yok.</p>
        {/if}
      {:else if aktifSekme === "magaza"}
        <h2>Mağaza</h2>
        {#if shopData.length}
          <div class="urun-kartlari">
            {#each shopData as urun}
              <div class="urun-kart">
                {#if urun.image_url}
                  <img
                    src={urun.image_url}
                    alt={urun.name}
                    class="kart-resim"
                  />
                {/if}
                <h3>{urun.name}</h3>
                <p class="kart-satici">
                  Satıcı: <strong>{urun.dealer_name}</strong>
                </p>
                <p class="kart-fiyat">{fiyatKolon(urun.price)}</p>
                <p class="kart-stok">Stok: {urun.stock}</p>
                <button class="sepet-btn">Sepete Ekle</button>
              </div>
            {/each}
          </div>
          {#if !hepsiYuklendi}
            <div bind:this={sentinel} class="sentinel">
              {#if yukleniyorShop}
                <div class="spinner"></div>
              {/if}
            </div>
          {/if}
        {:else}
          <p>Şu an satışta ürün yok.</p>
        {/if}
      {:else if aktifSekme === "profil"}
        <h2>Profilim</h2>
        <div class="profil-sayfa">
          <div class="profil-avatar">
            {#if profil.avatar_url}
              <img src={profil.avatar_url} alt="avatar" />
            {/if}
          </div>

          <div class="profil-bilgi">
            <label
              >Kullanıcı Adı
              <input value={profil.username} />
            </label>
            {#if role === "Admin"}
              <label>
                Rol<input value={profil.role} disabled />
              </label>
            {/if}
            <label
              >Adres
              <input bind:value={profil.address} placeholder="Adres" />
            </label>
            <label
              >Telefon
              <input bind:value={profil.phone} placeholder="Telefon" />
            </label>
            <label
              >Avatar URL
              <input
                bind:value={profil.avatar_url}
                placeholder="/avatars/..."
              />
            </label>
            <button class="ekle-btn" onclick={profilGuncelle}>Kaydet</button>
          </div>
        </div>
      {/if}
    </div>
  {/if}

  {#if error}<p class="error">{error}</p>
  {/if}
  {#if loading}<p>Yükleniyor... Lütfen Bekleyiniz...</p>{/if}

  {#if modalAcik}
    <div
      class="modal-arkaplan"
      onclick={() => (modalAcik = false)}
      onkeydown={(e) => e.key === "Escape" && (modalAcik = false)}
      role="button"
      tabindex="0"
    >
      <div
        class="modal"
        onclick={(e) => e.stopPropagation()}
        role="presentation"
      >
        <h3>Yeni Kullanıcı</h3>
        <input
          placeholder="Kullanıcı Adı"
          bind:value={yeniKullanici.username}
        />
        <input
          type="password"
          placeholder="Şifre"
          bind:value={yeniKullanici.password}
        />
        <input placeholder="Adres" bind:value={yeniKullanici.address} />
        <input placeholder="Telefon" bind:value={yeniKullanici.phone} />
        <select bind:value={yeniKullanici.role}>
          <option value="Kullanici">Kullanıcı</option>
          <option value="Bayi">Bayi</option>
          <option value="Admin">Admin</option>
        </select>
        <div class="modal-butonlar">
          <button class="iptal-btn" onclick={() => (modalAcik = false)}
            >İptal</button
          >
          <button class="ekle-btn" onclick={kullaniciEkle}>Ekle</button>
        </div>
      </div>
    </div>
  {/if}
  <!--
  Ürün Ekleme Modalı
  -->
  {#if urunModalAcik}
    <div
      class="modal-arkaplan"
      onclick={() => (urunModalAcik = false)}
      onkeydown={(e) => e.key === "Escape" && (urunModalAcik = false)}
      role="button"
      tabindex="0"
    >
      <div
        class="buyuk-modal"
        onclick={(e) => e.stopPropagation()}
        role="presentation"
      >
        <h2>{duzenlenenId ? "Ürün Düzenle" : "Yeni Ürün"}</h2>

        <label
          >Ürün Adı
          <input bind:value={urunForm.name} placeholder="Ürün adı" />
        </label>

        <label
          >Kategori
          <select bind:value={urunForm.category_id}>
            <option value="">Kategori seç</option>
            {#each categories.filter((c) => !c.parent_id) as ust}
              {#if categories.some((c) => c.parent_id === ust.id)}
                <optgroup label={ust.name}>
                  {#each categories.filter((c) => c.parent_id === ust.id) as alt}
                    <option value={alt.id}>{alt.name}</option>
                  {/each}
                </optgroup>
              {:else}
                <option value={ust.id}>{ust.name}</option>
              {/if}
            {/each}
          </select>
        </label>

        <label
          >Fiyat (₺)
          <input
            type="number"
            step="0.01"
            bind:value={urunForm.price}
            placeholder="0.00"
          />
        </label>

        <label>
          Resim <input bind:value={urunForm.image_url} placeholder="Ürün Adı" />
        </label>

        {#if urunForm.image_url}
          <img
            src={onizlemeYolu}
            alt="Önizleme"
            style="max-width: 150px; border-radius: 8px;"
          />
        {/if}
        <div class="modal-butonlar">
          <button class="iptal-btn" onclick={() => (urunModalAcik = false)}
            >İptal</button
          >
          <button class="ekle-btn" onclick={urunKaydet}
            >{duzenlenenId ? "Kaydet" : "Ekle"}</button
          >
        </div>
      </div>
    </div>
  {/if}

  <!-- Stok-fiyat Modal -->
  {#if islemModalAcik}
    <div
      class="modal-arkaplan"
      onclick={() => (islemModalAcik = false)}
      onkeydown={(e) => e.key === "Escape" && (islemModalAcik = false)}
      role="button"
      tabindex="0"
    >
      <div
        class="modal"
        onclick={(e) => e.stopPropagation()}
        role="presentation"
      >
        <h3>Stok-Fiyat Güncelle</h3>
        <!-- 2. Ürün seçimi (dropdown) -->
        <label
          >Ürün
          <select bind:value={secilenUrunId}>
            <option value="">Ürün Seçiniz</option>
            {#each myStock as p}
              <option value={p.product_id}>{p.name}</option>
            {/each}
          </select>
        </label>
        <!-- 3. İşlem türü seçimi (üç buton ya da select) -->
        <label
          >İşlem Türü
          <select class="islem-secim" bind:value={islemTuru}>
            <option disabled>İşlem türü seçiniz.</option>
            <option value="giris">Giriş</option>
            <option value="cikis">Çıkış</option>
            <option value="fiyat">Fiyat Güncelleme</option>
          </select>
        </label>

        <!-- 4. Türe göre değişen kısım: -->
        {#if secilenUrun && (islemTuru === "giris" || islemTuru === "cikis")}
          <p class="modal-bilgi">
            Mevcut stok: <strong>{secilenUrun.stock}</strong>
          </p>
          <label
            >Miktar
            <input
              type="number"
              min="1"
              bind:value={hareketMiktar}
              placeholder="0"
            />
          </label>
        {:else if secilenUrun && islemTuru === "fiyat"}
          <p class="modal-bilgi">
            Mevcut fiyat: <strong>{fiyatKolon(secilenUrun)}</strong>
          </p>
          <label
            >Yeni Fiyat
            <input
              type="number"
              step="0.01"
              bind:value={yeniFiyat}
              placeholder={secilenUrun.onerilen}
              class="fiyat-input {fiyatDurum ?? ''}"
            />
          </label>
          <p class="fiyat-ipucu">
            Aralık: {fiyatKolon(secilenUrun.alt_sinir)} - {fiyatKolon(
              secilenUrun.ust_sinir,
            )} · Önerilen:
            {fiyatKolon(secilenUrun.onerilen)}
          </p>
        {/if}

        <!-- 5. Hata mesajı: {#if modalHata} -->
        {#if modalHata}<p class="error">{modalHata}</p>{/if}

        <!-- 6. İptal / Kaydet butonları -->
        <div class="modal-butonlar">
          <button class="iptal-btn" onclick={() => (islemModalAcik = false)}
            >İptal</button
          >
          <button class="ekle-btn" onclick={islemKaydet}>Kaydet</button>
        </div>
      </div>
    </div>
  {/if}
</main>
