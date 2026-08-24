<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  import {
    API,
    oturum,
    durum,
    veri,
    authHeader,
    jsonHeader,
    fiyatKolon,
    sayfalar,
    sayfala,
    toplamSayfa,
    sayfaGit,
    sayfalariSifirla,
  } from "./lib/store.svelte.js";

  // Modallar
  import Login from "./lib/Login.svelte";

  // Sayfa Bileşenleri
  import BayiRaporlar from "./lib/BayiRaporlar.svelte";
  import BayiStok from "./lib/BayiStok.svelte";
  import AdminHareketler from "./lib/AdminHareketler.svelte";
  import AdminKategoriler from "./lib/AdminKategoriler.svelte";
  import AdminUrunler from "./lib/AdminUrunler.svelte";
  import AdminBayiler from "./lib/AdminBayiler.svelte";
  import AdminIstekler from "./lib/AdminIstekler.svelte";
  import AdminKullanicilar from "./lib/AdminKullanicilar.svelte";
  import Magaza from "./lib/Magaza.svelte";
  import Profil from "./lib/Profil.svelte";
  import BayiTalepler from "./lib/BayiTalepler.svelte";

  Chart.register(...registerables);

  let karsilama = $state(false);

  // Profil Sayfası
  let profil = $state({
    username: "",
    role: "",
    address: "",
    phone: "",
    avatar_url: "",
  });

  // Bayi / Admin / Kullanıcı Verileri
  let bekleyenSayi = $state(0);

  async function girisSonrasi() {
    if (oturum.role === "Bayi") {
      durum.aktifSekme = "anasayfa";
    } else if (oturum.role === "Admin") {
      durum.aktifSekme = "hareketler";
      await loadAll();
    } else if (oturum.role === "Kullanici") {
      durum.aktifSekme = "magaza";
    }
    karsilama = true;
    setTimeout(() => (karsilama = false), 1000);
  }

  function logout() {
    oturum.token = "";
    oturum.role = "";
    oturum.currentUser = "";
    oturum.avatarUrl = "";
    durum.aktifSekme = "anasayfa";
    veri.products = [];
    veri.myStock = [];
    veri.categories = [];
    sayfalariSifirla();
    localStorage.clear();
    location.reload();
  }

  async function loadAll() {
    durum.loading = true;
    durum.error = "";
    try {
      const [pRes, cRes] = await Promise.all([
        fetch(`${API}/api/products`),
        fetch(`${API}/api/categories`),
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

  onMount(async () => {
    if (!oturum.token) return;
    try {
      const res = await fetch(`${API}/api/profile`, {
        headers: { Authorization: oturum.token },
      });
      if (!res.ok) {
        logout();
        return;
      }
      const p = await res.json();
      oturum.role = p.role;
      oturum.currentUser = p.username;
      oturum.avatarUrl = p.avatar_url || "";
      localStorage.setItem("username", oturum.currentUser);
      localStorage.setItem("avatar_url", oturum.avatarUrl);
    } catch {
      logout();
      return;
    }

    if (oturum.role === "Bayi") {
      durum.aktifSekme = "anasayfa";
    } else if (oturum.role === "Admin") {
      durum.aktifSekme = "hareketler";
      loadAll();
    } else if (oturum.role === "Kullanici") {
      durum.aktifSekme = "magaza";
    }
  });
</script>

<main>
  {#if !oturum.token}
    <Login girisYapildi={girisSonrasi} />
  {:else if karsilama}
    <div class="karsilama-ekran">
      {#if oturum.avatarUrl}
        <img src={oturum.avatarUrl} alt="avatar" class="karsilama-avatar" />
      {/if}
      <h1>Hoş Geldiniz, {oturum.currentUser}!</h1>
      <div class="spinner"></div>
      <p>Sayfa Yükleniyor...</p>
    </div>
  {:else}
    <div class="toolbar">
      <div class="toolbar-ic">
        <button
          class="toolbar-baslik"
          onclick={() =>
            (durum.aktifSekme =
              oturum.role === "Admin" ? "hareketler" : "anasayfa")}
        >
          INTSHOP
        </button>

        <div class="toolbar-sekmeler">
          {#if oturum.role === "Admin"}
            <button
              class:aktif={durum.aktifSekme === "dealers"}
              onclick={() => (durum.aktifSekme = "dealers")}>Bayiler</button
            >
            <button
              class:aktif={durum.aktifSekme === "urunler"}
              onclick={() => (durum.aktifSekme = "urunler")}>Ürünler</button
            >
            <button
              class:aktif={durum.aktifSekme === "kategoriler"}
              onclick={() => (durum.aktifSekme = "kategoriler")}
              >Kategoriler</button
            >
            <button
              class:aktif={durum.aktifSekme === "istekler"}
              onclick={() => (durum.aktifSekme = "istekler")}
            >
              İstekler {#if bekleyenSayi > 0}<span class="badge"
                  >{bekleyenSayi}</span
                >{/if}
            </button>
            <button
              class:aktif={durum.aktifSekme === "users"}
              onclick={() => (durum.aktifSekme = "users")}>Kullanıcılar</button
            >
          {:else if oturum.role === "Bayi"}
            <button
              class:aktif={durum.aktifSekme === "stok"}
              onclick={() => (durum.aktifSekme = "stok")}>Stok</button
            >
            <button
              class:aktif={durum.aktifSekme === "raporlar"}
              onclick={() => (durum.aktifSekme = "raporlar")}>Raporlar</button
            >
            <button
              class:aktif={durum.aktifSekme === "talepler"}
              onclick={() => (durum.aktifSekme = "talepler")}>Talepler</button
            >
          {:else if oturum.role === "Kullanici"}
            <button
              class:aktif={durum.aktifSekme === "magaza"}
              onclick={() => (durum.aktifSekme = "magaza")}>Mağaza</button
            >
          {/if}
        </div>

        <div class="toolbar-right">
          <button
            class="profil-btn"
            onclick={() => (durum.aktifSekme = "profil")}
          >
            {#if oturum.avatarUrl}
              <img src={oturum.avatarUrl} alt="avatar" class="toolbar-avatar" />
            {/if}
            <span>{oturum.currentUser}</span>
          </button>
          <button class="cikis-btn" onclick={logout}>Çıkış</button>
        </div>
      </div>
    </div>

    {#if durum.bildirim}
      <div class="bildirim">{durum.bildirim}</div>
    {/if}

    <div class="sekme-icerik">
      {#if durum.aktifSekme === "anasayfa"}
        <div class="hosgeldin">
          <h1>Hoş Geldiniz, {oturum.currentUser}!</h1>
          <p>
            Stok işlemleriniz için "Stok", raporlarınız için "Raporlar"
            sekmesini kullanın.
          </p>
        </div>
      {:else if durum.aktifSekme === "stok"}
        <BayiStok />
      {:else if durum.aktifSekme === "raporlar"}
        <BayiRaporlar />
      {:else if durum.aktifSekme === "kategoriler"}
        <AdminKategoriler yenile={loadAll}/>
      {:else if durum.aktifSekme === "istekler"}
        <AdminIstekler sayiDegisti={(n) => (bekleyenSayi = n)} />
      {:else if durum.aktifSekme === "hareketler"}
        <AdminHareketler />
      {:else if durum.aktifSekme === "talepler"}
        <BayiTalepler />
      {:else if durum.aktifSekme === "urunler"}
        <AdminUrunler yenile={loadAll}/>
      {:else if durum.aktifSekme === "dealers"}
        <AdminBayiler />
      {:else if durum.aktifSekme === "users"}
        <AdminKullanicilar />
      {:else if durum.aktifSekme === "profil"}
        <Profil />
      {:else if durum.aktifSekme === "magaza"}
        <Magaza />
      {/if}
    </div>
  {/if}

  {#if durum.error}<p class="error">{durum.error}</p>{/if}
</main>
