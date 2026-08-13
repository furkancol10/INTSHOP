<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  Chart.register(...registerables);

  const API = import.meta.env.VITE_API_URL ?? "http://localhost:5081";
  //
  // oturum
  //
  let token = $state(localStorage.getItem("token") || "");
  let role = $state(localStorage.getItem("role") || "");
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
      localStorage.setItem("role", role);
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
    if (!e) return null;   // boşsa henüz bir şey söyleme
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

  onMount(() => {
    if (!token) return;
    console.log(
      "onMount role:",
      role,
      "→ aktifSekme olacak:",
      role === "Admin" ? "hareketler" : "anasayfa",
    );
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
                <p class="ipucu-hata">Geçerli bir E-Posta giriniz. (ornek@site.com)</p>
              {/if}
              <div class="input-group">
                <input placeholder="Adres" bind:value={kayitForm.address} />
              </div>
              <div class="input-group">
                <input placeholder="Telefon" bind:value={kayitForm.phone} />
              </div>
              <button class="login-btn" onclick={kayitOl} >Kayıt Ol</button>
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

        </div><!-- Ic -->
      </div><!-- Cerceve -->
    </div> <!-- Wrapper -->
  
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
            class:aktif={aktifSekme === "fiyatlandirma"}
            onclick={() => (aktifSekme = "fiyatlandirma")}>Fiyatlandırma</button
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
          <table>
            <thead>
              <tr
                ><th>Ürün</th><th>Kategori</th><th>Stok</th><th
                  >Giriş / Çıkış</th
                ></tr
              >
            </thead>
            <tbody>
              {#each myStock as p}
                <tr class:dusuk={p.stock < 10}>
                  <td>{p.name}</td>
                  <td>{p.category}</td>
                  <td>{p.stock}</td>
                  <td>
                    <input
                      type="number"
                      min="1"
                      placeholder="Miktar"
                      bind:value={miktarlar[p.product_id]}
                      style="width: 80px"
                    />
                    <button
                      onclick={() =>
                        movement(
                          p.product_id,
                          Math.abs(Number(miktarlar[p.product_id]) || 0),
                        )}>Giriş</button
                    >
                    <button
                      onclick={() =>
                        movement(
                          p.product_id,
                          -Math.abs(Number(miktarlar[p.product_id]) || 0),
                        )}>Çıkış</button
                    >
                  </td>
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
      {:else if aktifSekme === "indirim"}
        <h2>İndirimler</h2>
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
                    <td>{p.category}</td>
                    <td>{p.stock}</td>
                    <td>{p.price} ₺</td>
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
      {:else if aktifSekme === "indirimAyar"}
        <h2>İndirim Ayarları</h2>
        <p>Bu bölüm yakında eklenecek.</p>
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
                <p class="kart-fiyat">{urun.price} ₺</p>
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
            {#each categories as c}
              <option value={c.id}>{c.name}</option>
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
</main>
