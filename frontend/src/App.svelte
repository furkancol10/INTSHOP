<script>
  import { onMount } from 'svelte';
  import { Chart, registerables } from 'chart.js';
  Chart.register(...registerables);

  const API = import.meta.env.VITE_API_URL ?? 'http://localhost:5081';

  // oturum
  let token = $state(localStorage.getItem('token') || '');
  let role = $state(localStorage.getItem('role') || '');
  let currentUser = $state(localStorage.getItem('username') || '');

  // Giriş
  let loginUser = $state('');
  let loginPass = $state('');
  let loginError = $state('');

  async function login() {
    loginError = '';
    try {
      const res = await fetch(`${API}/api/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: loginUser, password: loginPass})
      });
      if (!res.ok) throw new Error('Kullanıcı adı veya şifre hatalı');
      const data = await res.json();
      token = data.token;
      role = data.role;
      currentUser = data.username;
      localStorage.setItem('token', token);
      localStorage.setItem('role', role);
      localStorage.setItem('username', currentUser);
      if (role === 'Bayi') { await loadMyStock(); await loadHistory();}
      else await loadAll();
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    }
  }

  function logout() {
    token = ''; role = ''; currentUser = '';
    localStorage.clear();
  }

  let products = $state([]);
  let categories = $state([]);
  let error = $state('');
  let loading = $state(true);

  // form alanları
  let name = $state('');
  let categoryId = $state('');
  let stock = $state(0);
  let price = $state(0);

  let miktarlar = $state({});
  let history = $state([]);

  let chartCanvas = $state(null);
  let chartInstance = null;

  async function loadAll() {
    loading = true; error = '';
    try {
      const [pRes, cRes] = await Promise.all([
        fetch(`${API}/api/products`),
        fetch(`${API}/api/categories`)
      ]);
      if (!pRes.ok || !cRes.ok) throw new Error('Veri alınamadı');
      products = await pRes.json();
      categories = await cRes.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    } finally {
      loading = false;
    }
  }

  async function addProduct() {
    if (!name || !categoryId) { error = 'Ad ve kategori zorunlu'; return; }
    try {
      const res = await fetch(`${API}/api/products`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': token
         },
        body: JSON.stringify({
          name,
          category_id: Number(categoryId),
          stock: Number(stock),
          price: Number(price)
        })
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      name = ''; categoryId = ''; stock = 0; price = 0;
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function deleteProduct(id) {
    try {
      const res = await fetch(`${API}/api/products/${id}`, { 
        method: 'DELETE',
        headers: { 'Authorization': token }
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  //bayi oturumu
  let myStock = $state([]);

  async function loadMyStock() {
    try {
      const res = await fetch(`${API}/api/my-stock`, {
        headers: { 'Authorization': token }
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      myStock = await res.json();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }finally {
      loading = false;
    }

  }

  async function movement(productId, change) {
    try {
      const res = await fetch(`${API}/api/my-stock/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': token },
        body: JSON.stringify({ product_id: productId, change })
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || `HTTP ${res.status}`);
      }
      miktarlar[productId] = '';
      await loadMyStock();
      await loadHistory();
    }catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadHistory() {
    try {
      const res = await fetch(`${API}/api/my-stock/history`, {
        headers: { 'Authorization': token }
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

    if(chartInstance) chartInstance.destroy();

    chartInstance = new Chart(chartCanvas, {
      type: 'bar',
      data: {
        labels: history.map(h=> new Date(h.tarih).toLocaleDateString('tr-TR')),
        datasets: [
          {
            label: 'Giriş',
            data: history.map(h => h.giris),
            backgroundColor: '#22a722'
          },
          {
            label: 'Çıkış',
            data: history.map(h => h.cikis),
            backgroundColor: '#c00'
          }
        ]
      },
      options: {
        responsive: true,
        scales: { y: { beginAtZero: true } }
      }
    });
  }

  onMount(() => { 
    if (!token) return;
    if(role === 'Bayi') {loadMyStock(); loadHistory();}
    else loadAll();
   });
</script>

<main>
  {#if !token}
    <div class="login">
      <h1>Giriş</h1>
      <input placeholder="Kullanıcı adı" bind:value={loginUser} />
      <input type="password" placeholder="Şifre" bind:value={loginPass} />
      <button onclick={login}>Giriş Yap</button>
      {#if loginError}<p class="error">{loginError}</p>{/if}
    </div>
  {:else}
    <div class="topbar">
      <h1>Stok Paneli</h1>
      <span>{currentUser} ({role}) <button onclick={logout}>Çıkış</button></span>
    </div>

  {#if role === 'Bayi'}
    <h2>Stok Yönetimi</h2>
    <table>
      <thead>
        <tr><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Giriş / Çıkış</th></tr>
      </thead>
      <tbody>
        {#each myStock as p}
          <tr class:dusuk={p.stock < 10}>
            <td>{p.name}</td>
            <td>{p.category}</td>
            <td>{p.stock}</td>
            <td>
              <input type="number" min="1" placeholder="Miktar" bind:value={miktarlar[p.product_id]} style="width: 80px;"/>
              <button onclick={() => movement(p.product_id, Math.abs(Number(miktarlar[p.product_id]) || 0))}>Giriş</button>
              <button onclick={() => movement(p.product_id, -Math.abs(Number(miktarlar[p.product_id]) || 0))}>Çıkış</button>
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
    
    <div style="display: flex; gap:2rem; align-items: flex-start; flex-wrap: wrap;">
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
                  <td>{new Date(h.tarih).toLocaleDateString('tr-TR')}</td>
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

  {#if role == "Admin"}
    <div class="form">
      <input placeholder="Ürün adı" bind:value={name} />
      <select bind:value={categoryId}>
        <option value="">Kategori seç</option>
        {#each categories as c}
          <option value={c.id}>{c.name}</option>
        {/each}
      </select>
      <input type="number" placeholder="Stok" bind:value={stock} />
      <input type="number" step="0.01" placeholder="Fiyat" bind:value={price} />
      <button onclick={addProduct}>Ekle</button>
    </div>
  {/if}

  {#if error}<p class="error">{error}</p> {/if}
  {#if loading}<p>Yükleniyor... Lütfen Bekleyiniz...</p>{/if}

  {#if products.length}
    <p>{products.length} ürün</p>
    <table>
      <thead>
        <tr>
          <th>ID</th><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Fiyat</th>
          {#if role == 'Admin'}<th></th>{/if}
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
            {#if role === 'Admin'}
              <td><button class="sil" onclick={() => deleteProduct(p.id)}>Sil</button></td>
            {/if}
          </tr>          
        {/each}
      </tbody>
    </table>
  {/if}
{/if}
</main>