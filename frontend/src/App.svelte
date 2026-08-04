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

  //bayi oturumu
  let myStock = $state([]);

  //admin oturumu
  let movements = $state([]);

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
        await loadHistory();
      } else { await loadAll(); await loadMovements(); }
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    }
  }

  function logout() {
    token = "";
    role = "";
    currentUser = "";
    localStorage.clear();
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

  async function loadMovements() {
    try {
      const res = await fetch(`${API}/api/my-stock/movements`, {
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

  onMount(() => {
    if (!token) return;
    if (role === "Bayi") {
      loadMyStock();
      loadHistory();
    } else { loadAll(); loadMovements(); }
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
    <div class="topbar">
      <h1>Stok Paneli</h1>
      
    </div>

    {#if role === "Bayi"}
      <h2>Stok Yönetimi</h2>
      <table>
        <thead>
          <tr
            ><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Giriş / Çıkış</th
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
                  style="width: 80px;"
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

      <div
        style="display: flex; gap:2rem; align-items: flex-start; flex-wrap: wrap;"
      >
        <div style="flex: 1; min-width: 200px;">
          <h2>Giriş / Çıkış Geçmişi</h2>
          {#if history.length}
            <table>
              <thead>
                <tr><th>Tarih</th><th>Giriş</th><th>Çıkış</th></tr>
              </thead>
              <tbody>
                {#each history as h}
                  <tr>
                    <td>{new Date(h.tarih).toLocaleDateString("tr-TR")}</td>
                    <td style="color: greenyellow;">+{h.giris}</td>
                    <td style="color: #c00;">-{h.cikis}</td>
                  </tr>
                {/each}
              </tbody>
            </table>
          {:else}
            <p>Henüz giriş / çıkış yapılmamış.</p>
          {/if}
        </div>
        <div style="flex: 1; min-width: 350px;">
          <h2>Giriş / Çıkış Grafiği</h2>
          {#if history.length}
            <div style="max-width: 600px;">
              <canvas bind:this={chartCanvas}></canvas>
            </div>
          {/if}
        </div>
      </div>
    {/if}

    {#if role == 'Admin'}
      <div class="toolbar">
        <button class:aktif={aktifSekme === 'bayiler'} onclick={() => aktifSekme = 'bayiler'}>Bayiler</button>
        <button class:aktif={aktifSekme === 'urunler'} onclick={() => aktifSekme = 'urunler'}>Ürünler</button>
        <button class:aktif={aktifSekme === 'indirim'} onclick={() => aktifSekme = 'indirim'}>İndirim</button>
        <button class:aktif={aktifSekme === 'kullanicilar'} onclick={() => aktifSekme = 'kullanicilar'}>Kullanıcılar</button>
        <span class="toolbar-spacer"></span>
        <span class="toolbar-user">{currentUser}</span>
        <button class="cikis-btn" onclick={logout}>Çıkış</button>
      </div>

      <div class="sekme-icerik">
        {#if aktifSekme === 'urunler'}
          <h2>Ürünler</h2>
          {#if products.length}
            <table>
              <thead>
                <tr>
                  <th>ID</th><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Fiyat</th>
                </tr>
              </thead>
              <tbody>
                {#each products as p}
                  <tr class:dusuk={p.stock < 10}>
                    <td>{p.id}</td>
                    <td>{p.name}</td>
                    <td>{p.category}</td>
                    <td>{p.stock}</td>
                    <td>{p.price} ₺</td>
                    <td><button class="sil" onclick={() => deleteProduct(p.id)}>Sil</button></td>
                  </tr>
                {/each}
              </tbody>
            </table>
          {/if}
          {:else if aktifSekme === 'bayiler'}
            <h2>Bayiler</h2>
            <!-- Bayiler tab content -->
          {:else if aktifSekme === 'indirim'}
            <h2>İndirim</h2>
            <!-- İndirim tab content -->
          {:else if aktifSekme === 'kullanicilar'}
            <h2>Kullanıcılar</h2>
            <!-- Kullanıcılar tab content -->
          {/if}
      </div>
    {/if}

    {#if error}<p class="error">{error}</p>
    {/if}
    {#if loading}<p>Yükleniyor... Lütfen Bekleyiniz...</p>{/if}

    {#if products.length}
      <p>{products.length} ürün</p>
      <table>
        <thead>
          <tr>
            <th>ID</th><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Fiyat</th>
            {#if role == "Admin"}<th></th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each products as p}
            <tr class:dusuk={p.stock < 10}>
              <td>{p.id}</td>
              <td>{p.name}</td>
              <td>{p.category}</td>
              <td>{p.stock}</td>
              <td>{p.price} ₺</td>
              {#if role === "Admin"}
                <td
                  ><button class="sil" onclick={() => deleteProduct(p.id)}
                    >Sil</button
                  ></td
                >
              {/if}
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}
  {/if}
</main>
