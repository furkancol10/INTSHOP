<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  import {
    API,
    oturum,
    durum,
    veri,
    sayfalariSifirla,
    sepetYukle,
    authHeader,
  } from "./lib/store.svelte.js";

  // Modallar
  import Login from "./lib/Components/Login.svelte";
  import ParolaModal from "./lib/Modals/ParolaModal.svelte";

  // Sayfa Bileşenleri
  import BayiRaporlar from "./lib/Components/BayiRaporlar.svelte";
  import BayiStok from "./lib/Components/BayiStok.svelte";
  import AdminHareketler from "./lib/Components/AdminHareketler.svelte";
  import AdminKategoriler from "./lib/Components/AdminKategoriler.svelte";
  import AdminUrunler from "./lib/Components/AdminUrunler.svelte";
  import AdminBayiler from "./lib/Components/AdminBayiler.svelte";
  import AdminIstekler from "./lib/Components/AdminIstekler.svelte";
  import AdminKullanicilar from "./lib/Components/AdminKullanicilar.svelte";
  import Magaza from "./lib/Components/Magaza.svelte";
  import UrunDetay from "./lib/Components/UrunDetay.svelte";
  import Profil from "./lib/Components/Profil.svelte";
  import BayiTalepler from "./lib/Components/BayiTalepler.svelte";
  import AdminLoglar from "./lib/Components/AdminLoglar.svelte";
  import AdminDenetim from "./lib/Components/AdminDenetim.svelte";
  import Sepet from "./lib/Components/Sepet.svelte";

  Chart.register(...registerables);

  let karsilama = $state(false);

  // Bayi / Admin / Kullanıcı Verileri
  let bekleyenSayi = $state(0);

  async function girisSonrasi() {
    if (oturum.role === "Bayi") {
      durum.aktifSekme = "stok";
    } else if (oturum.role === "Admin") {
      durum.aktifSekme = "loglar";
      await loadAll();
    } else if (oturum.role === "Kullanici") {
      durum.aktifSekme = "magaza";
    }
    karsilama = true;
    setTimeout(() => (karsilama = false), 1000);
  }

  async function logout() {
    try {
      await fetch(`${API}/api/logout`, {
        method: "POST",
        headers: { Authorization: oturum.token },
      });
    } catch {
      // sunucuya ulaşılamasa bile yerel çıkış yapılmalı
    }

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
      oturum.sifreDegistir = p.must_change_password || false;
      localStorage.setItem("username", oturum.currentUser);
      localStorage.setItem("avatar_url", oturum.avatarUrl);
    } catch {
      logout();
      return;
    }

    if (oturum.role === "Bayi") {
      durum.aktifSekme = "stok";
    } else if (oturum.role === "Admin") {
      durum.aktifSekme = "loglar";
      loadAll();
    } else if (oturum.role === "Kullanici") {
      durum.aktifSekme = "magaza";
      sepetYukle();
    }
  });
</script>

<main>
  {#if !oturum.token}
    <Login girisYapildi={girisSonrasi} />
  {:else if oturum.sifreDegistir}
    <ParolaModal />
  {:else if karsilama}<div class="min-h-screen flex flex-col items-center justify-center gap-[1.2rem] bg-[repeating-radial-gradient(circle,blue,lightseagreen,lightblue,whitesmoke)] text-white">
      {#if oturum.avatarUrl}
        <img src={oturum.avatarUrl} alt="avatar" class="w-[120px] h-[120px] rounded-full object-cover border-4 border-white/60 shadow-[0_8px_32px_rgba(255,255,255,0.2)]" />
      {/if}
      <h1 class="m-0 text-[2rem] [text-shadow:0_2px_8px_rgba(255,255,255,0.2)]">Hoş Geldiniz, {oturum.currentUser}!</h1>
      <div class="spinner"></div>
      <p class="m-0 opacity-90">Sayfa Yükleniyor...</p>
    </div>{:else if !oturum.role}{:else}
    <div class="flex items-center gap-2 bg-teal-600 text-slate-100">
      <div class="justify-between flex items-center w-full px-8 py-3">
        <button
          class="bg-transparent border-none font-bold text-xl text-slate-100 cursor-pointer mr-6 px-3 py-2 rounded-lg tracking-wide transition hover:bg-white/10"
          onclick={() =>
            (durum.aktifSekme =
              oturum.role === "Admin" ? "loglar" : "anasayfa")}
        >
          INTSHOP
        </button>

        <div class="flex items-center gap-2">
          {#if oturum.role === "Admin"}
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'dealers' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "dealers")}>Bayiler</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'urunler' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "urunler")}>Ürünler</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'kategoriler' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "kategoriler")}
              >Kategoriler</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'istekler' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "istekler")}
            >
              İstekler {#if bekleyenSayi > 0}<span class="bg-coral text-white rounded-full px-[.4rem] py-[.1rem] text-[.7rem] ml-1"
                  >{bekleyenSayi}</span
                >{/if}
            </button>
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'users' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "users")}>Kullanıcılar</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'hareketler' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "hareketler")}
              >Hareketler</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'denetim' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "denetim")}
              >Denetim</button
            >
          {:else if oturum.role === "Bayi"}
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'stok' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "stok")}>Stok</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'raporlar' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "raporlar")}>Raporlar</button
            >
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'talepler' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "talepler")}>Taleplerim</button
            >
          {:else if oturum.role === "Kullanici"}
            <button
              class="relative px-4 py-2 border-none rounded-md cursor-pointer transition {durum.aktifSekme === 'magaza' ? 'bg-slate-100 text-teal-600 font-semibold' : 'bg-transparent text-slate-100'}"
              onclick={() => (durum.aktifSekme = "magaza")}>Mağaza</button
            >
          {/if}
        </div>

        <div class="flex items-center gap-2">
          <button
            class="flex items-center gap-2 bg-transparent border-none text-white cursor-pointer px-8 py-1 rounded-lg transition hover:bg-white/15"
            onclick={() => (durum.aktifSekme = "profil")}
          >
            {#if oturum.avatarUrl}
              <img src={oturum.avatarUrl} alt="avatar" class="w-8 h-8 rounded-full object-cover" />
            {/if}
            <span>{oturum.currentUser}</span>
          </button>
          <button class="bg-coral text-white px-4 py-2 rounded-md cursor-pointer border-none text-[.95rem]" onclick={logout}>Çıkış</button>
        </div>
      </div>
    </div>

    {#if durum.bildirim}
      <div class="fixed top-[70px] right-5 bg-green-600 text-white p-3 rounded-lg shadow-[0_4px_16px_rgba(0,0,0,0.2)] z-[200]">{durum.bildirim}</div>
    {/if}

    <div class="px-6 py-8 mx-auto max-w-[1440px]">
      {#if durum.aktifSekme === "loglar"}
        <AdminLoglar />
      {:else if durum.aktifSekme === "stok"}
        <BayiStok />
      {:else if durum.aktifSekme === "raporlar"}
        <BayiRaporlar />
      {:else if durum.aktifSekme === "kategoriler"}
        <AdminKategoriler yenile={loadAll} />
      {:else if durum.aktifSekme === "istekler"}
        <AdminIstekler sayiDegisti={(n) => (bekleyenSayi = n)} />
      {:else if durum.aktifSekme === "hareketler"}
        <AdminHareketler />
      {:else if durum.aktifSekme === "denetim"}
        <AdminDenetim />
      {:else if durum.aktifSekme === "talepler"}
        <BayiTalepler />
      {:else if durum.aktifSekme === "urunler"}
        <AdminUrunler yenile={loadAll} />
      {:else if durum.aktifSekme === "dealers"}
        <AdminBayiler />
      {:else if durum.aktifSekme === "users"}
        <AdminKullanicilar />
      {:else if durum.aktifSekme === "profil"}
        <Profil />
      {:else if durum.aktifSekme === "magaza"}
        <Magaza />
      {:else if durum.aktifSekme === "urun-detay"}
        <UrunDetay
          productId={durum.secilenUrunId}
          geriDon={() => (durum.aktifSekme = "magaza")}
        />
      {:else if durum.aktifSekme === "sepet"}
        <Sepet />
      {/if}
    </div>
  {/if}

  {#if durum.error}<p class="text-red-700 text-sm m-0">{durum.error}</p>{/if}
</main>
