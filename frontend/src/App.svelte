<script>
  import { onMount } from 'svelte';

  const API = import.meta.env.VITE_API_URL ?? 'http://localhost:5081';

  let products = $state([]);
  let categories = $state([]);
  let error = $state('');
  let loading = $state(true);

  // form alanları
  let name = $state('');
  let categoryId = $state('');
  let stock = $state(0);
  let price = $state(0);

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
        headers: { 'Content-Type': 'application/json' },
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
      const res = await fetch(`${API}/api/products/${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await loadAll();
    } catch (e) {
      error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(loadAll);
</script>

<main>
  <h1>Stok Paneli</h1>

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

  {#if error}<p class="error">{error}</p>{/if}
  {#if loading}<p>Yükleniyor...</p>{/if}

  {#if products.length}
    <p>{products.length} ürün</p>
    <table>
      <thead>
        <tr>
          <th>ID</th><th>Ürün</th><th>Kategori</th><th>Stok</th><th>Fiyat</th><th></th>
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
</main>