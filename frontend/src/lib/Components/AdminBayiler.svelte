<script>
  import { onMount } from "svelte";
  import { API, authHeader, durum, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";

  let dealers = $state([]);

  async function loadDealers() {
    try {
      const res = await fetch(`${API}/api/dealers`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      dealers = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(loadDealers);
</script>

<h2>Bayiler</h2>
{#if dealers.length}
  <div class="tablo-cerceve">
    <table>
      <thead>
        <tr><th>ID</th><th>Bayi Adı</th><th>Adres</th><th>Telefon</th></tr>
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
  </div>
  <div class="pagination">
    <button onclick={() => sayfaGit("dealers", -1)}
            disabled={(sayfalar.dealers ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.dealers ?? 1} / {toplamSayfa(dealers)}</span>
    <button onclick={() => sayfaGit("dealers", 1)}
            disabled={(sayfalar.dealers ?? 1) === toplamSayfa(dealers)}>Sonraki</button>
  </div>
{:else}
  <p>Henüz bayi yok.</p>
{/if}