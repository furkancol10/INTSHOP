<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  Chart.register(...registerables);

  const API = import.meta.env.VITE_API_URL ?? "http://localhost:5081";

  // oturum
  let token = $state(localStorage.getItem("token") || "");
  let role = $state(localStorage.getItem("role") || "");
  let currentUser = $state(localStorage.getItem("username") || "");

  // veriler
  let products = $state([]);
  let categories = $state([]);
  let error = $state("");
  let loading = $state(true);

  let name = $state("");
  let categoryId = $state("");
  let stock = $state(0);
  let price = $state(0);

  // Geçmiş tablosu
  let miktarlar = $state({});
  let history = $state([]);

  // Grafik
  let chartCanvas = $state(null);
  let chartInstance = null;

  //Toolbar
  let aktifSekme = $state("hareketler");

  // Giriş
  let loginUser = $state("");
  let loginPass = $state("");
  let loginError = $state("");

  //Kullanıcı ekleme
  let modalAcik = $state(false);
  let yeniKullanici = $state({
    username: "",
    password: "",
    role: "",
    address: "",
    phone: "",
  });

  //Ürün Ekleme
  let urunModalAcik = $state(false);
  let duzenlenenId = $state(false);
  let urunForm = $state({
    name: "",
    category_id: "",
    price: "",
    image_url: "",
  });

  //bayi oturumu
  let myStock = $state([]);
  let dealers = $state([]);
  let myMovements = $state([]);

  //admin oturumu
  let movements = $state([]);

  //Kullanıcı oturumu
  let users = $state([]);
  let shopData = $state([]);
  let seciliBayiler = $state({});

  //Sayfalama
  let mainPage = $state(1);
  const pageSize = 10;
  let aktifHareketler = $derived(role === "Admin" ? movements : myMovements);
  let movementPage = $derived(
    aktifHareketler.slice((mainPage - 1) * pageSize, mainPage * pageSize),
  );
  let totalPages = $derived(Math.ceil(aktifHareketler.length / pageSize));

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
      localStorage.setItem("token", token);
      localStorage.setItem("role", role);
      localStorage.setItem("username", currentUser);
      if (role === "Bayi") {
        await loadMyStock();
        await loadMyMovements();
      } else if (role == "Admin") {
        await loadAll();
        await loadMovements();
        await loadDealers();
        await loadUsers();
      }
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    }
  }

  function logout() {
    token = "";
    role = "";
    currentUser = "";
    aktifSekme = "anasayfa";
    products = [];
    movements = [];
    myStock = [];
    history = [];
    dealers = [];
    users = [];
    localStorage.clear();
    location.reload();
  }

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
    try {
      const res = await fetch(`${API}/api/shop`, {
        headers: { Authorization: token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      shopData = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    } finally {
      loading = false;
    }
  }

  let gruplananUrunler = $derived.by(() => {
    const map = {};
    for (const row of shopData) {
      if (!map[row.product_id]) {
        map[row.product_id] = {
          product_id: row.product_id,
          name: row.name,
          price: row.price,
          image_url: row.image_url,
          bayiler: [],
        };
      }
      map[row.product_id].bayiler.push({
        dealer_id: row.dealer_id,
        dealer_name: row.dealer_name,
        stock: row.stock,
      });
    }
    return Object.values(map);
  });

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
    try {
      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json", Authorization: token },
        body: JSON.stringify({
          name: urunForm.name,
          category_id: Number(urunForm.category_id),
          price: Number(urunForm.price),
          image_url: urunForm.image_url,
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
      loadShop();
    }
  });
</script>

<main>
  {#if !token}
    <div class="login-wrapper">
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
        {#if loginError}<p class="error">{loginError}</p>{/if}
      </div>
    </div>
  {:else}
    <div class="toolbar">
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
          class:aktif={aktifSekme === "indirim"}
          onclick={() => (aktifSekme = "indirim")}>İndirim</button
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
          class:aktif={aktifSekme === "indirim"}
          onclick={() => (aktifSekme = "indirim")}>İndirim</button
        >
      {:else if role === "Kullanici"}
        <button
          class:aktif={aktifSekme === "magaza"}
          onclick={() => (aktifSekme = "magaza")}>Mağaza</button
        >
      {/if}

      <span class="toolbar-spacer"></span>
      <span class="toolbar-user">{currentUser} ({role})</span>
      <button class="cikis-btn" onclick={logout}>Çıkış</button>
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
                  {#each movementPage as m}
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
                  onclick={() => (mainPage = mainPage - 1)}
                  disabled={mainPage === 1}>Önceki</button
                >
                <span>Sayfa {mainPage} / {totalPages}</span>
                <button
                  onclick={() => (mainPage = mainPage + 1)}
                  disabled={mainPage === totalPages}>Sonraki</button
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
                {#each movementPage as m}
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
              onclick={() => (mainPage = mainPage - 1)}
              disabled={mainPage === 1}>Önceki</button
            >
            <span>Sayfa {mainPage} / {totalPages}</span>
            <button
              onclick={() => (mainPage = mainPage + 1)}
              disabled={mainPage === totalPages}>Sonraki</button
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
                {#each products as p}
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
                onclick={() => (mainPage = mainPage - 1)}
                disabled={mainPage === 1}>Önceki</button
              >
              <span>Sayfa {mainPage} / {totalPages}</span>
              <button
                onclick={() => (mainPage = mainPage + 1)}
                disabled={mainPage === totalPages}>Sonraki</button
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
                {#each dealers as d}
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
                onclick={() => (mainPage = mainPage - 1)}
                disabled={mainPage === 1}>Önceki</button
              >
              <span>Sayfa {mainPage} / {totalPages}</span>
              <button
                onclick={() => (mainPage = mainPage + 1)}
                disabled={mainPage === totalPages}>Sonraki</button
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
                {#each users as u}
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
                onclick={() => (mainPage = mainPage - 1)}
                disabled={mainPage === 1}>Önceki</button
              >
              <span>Sayfa {mainPage} / {totalPages}</span>
              <button
                onclick={() => (mainPage = mainPage + 1)}
                disabled={mainPage === totalPages}>Sonraki</button
              >
            </div>
          </div>
        {:else}
          <p>Henüz kullanıcı yok.</p>
        {/if}
      {:else if aktifSekme === "magaza"}
        <h2>Mağaza</h2>
        {#if gruplananUrunler.length}
          <div class="urun-kartlari">
            {#each gruplananUrunler as urun}
              <div class="urun-kart">
                {#if urun.image_url}
                  <img
                    src={urun.image_url}
                    alt={urun.name}
                    class="kart-resim"
                  />
                {/if}
                <h3>{urun.name}</h3>
                <p class="kart-fiyat">{urun.price} ₺</p>
                <label
                  >Satıcı:
                  <select bind:value={seciliBayiler[urun.product_id]}>
                    <option value="">Bayi Seçiniz ...</option>
                    {#each urun.bayiler as b}
                      <option value={b.dealer_id}
                        >{b.dealer_name} (stok: {b.stock})</option
                      >
                    {/each}
                  </select>
                </label>
              </div>
            {/each}
          </div>
        {:else}
          <p>Şu an satışta ürün yok.</p>
        {/if}
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
          Resim URL<input
            bind:value={urunForm.image_url}
            placeholder="https://..."
          />
        </label>

        {#if urunForm.image_url}
          <img
            src={urunForm.image_url}
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
