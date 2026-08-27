<script>
  import { onMount } from "svelte";
  import { API, oturum, durum, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";

  let movements = $state([]);

  async function loadMovements() {
    try {
      const res = await fetch(`${API}/api/movements`, {
        headers: { Authorization: oturum.token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      movements = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(loadMovements);
</script>

<h2>Bayi Hareketleri</h2>

{#if movements.length}
  <div class="tablo-cerceve">
    <table>
      <thead>
        <tr>
          <th>Bayi</th>
          <th>Ürün</th>
          <th>İşlem</th>
          <th>Miktar</th>
          <th>Tarih</th>
        </tr>
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
      disabled={(sayfalar.hareketler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.hareketler ?? 1} / {toplamSayfa(movements)}</span>
    <button
      onclick={() => sayfaGit("hareketler", 1)}
      disabled={(sayfalar.hareketler ?? 1) === toplamSayfa(movements)}>Sonraki</button>
  </div>
{:else}
  <p>Henüz hareket yok.</p>
{/if}