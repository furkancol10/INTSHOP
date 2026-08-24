<script>
  import { onMount } from "svelte";
  import { API, authHeader, jsonHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "./store.svelte.js";
  import RedModal from "./RedModal.svelte";

  let { sayiDegisti } = $props();

  let requests = $state([]);
  let filtre = $state("all");
  let redModalAcik = $state(false);
  let redTalepId = $state(null);

  async function loadRequests() {
    try {
      const res = await fetch(`${API}/api/requests?status=${filtre}`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      requests = await res.json();
      sayiDegisti?.(requests.filter((r) => r.status === "pending").length);
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  async function talepKarar(id, karar, not = "") {
    try {
      const res = await fetch(`${API}/api/requests/${id}/${karar}`, {
        method: "PUT",
        headers: jsonHeader(),
        body: JSON.stringify({ note: not }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      await loadRequests();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function filtreDegistir(yeni) {
    filtre = yeni;
    loadRequests();
  }

  onMount(loadRequests);
</script>

<h2>Fiyat Değişiklik İstekleri</h2>

<div class="filtre-satir">
  <button class:aktif={filtre === "all"} onclick={() => filtreDegistir("all")}>Hepsi</button>
  <button class:aktif={filtre === "pending"} onclick={() => filtreDegistir("pending")}>Bekleyen</button>
  <button class:aktif={filtre === "approved"} onclick={() => filtreDegistir("approved")}>Onaylanan</button>
  <button class:aktif={filtre === "rejected"} onclick={() => filtreDegistir("rejected")}>Reddedilen</button>
</div>

{#if requests.length}
  <div class="tablo-cerceve">
    <table>
      <thead>
        <tr>
          <th>Bayi</th><th>Ürün</th><th>Eski Fiyat</th><th>Yeni Fiyat</th>
          <th>Aralık</th><th>Tarih</th><th>Durum</th><th>İşlem</th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(requests, "istekler") as r}
          <tr>
            <td>{r.bayi}</td>
            <td>{r.urun}</td>
            <td>{fiyatKolon(r.old_price)}</td>
            <td><strong>{fiyatKolon(r.new_price)}</strong></td>
            <td class="kucuk">{fiyatKolon(r.alt_sinir)} - {fiyatKolon(r.ust_sinir)}</td>
            <td class="kucuk">{new Date(r.created_at).toLocaleString("tr-TR")}</td>
            <td>
              {#if r.status === "pending"}
                <span class="durum bekliyor">Bekliyor</span>
              {:else if r.status === "approved"}
                <span class="durum onayli">Onaylandı</span>
              {:else if r.status === "rejected"}
                <span class="durum redli">Reddedildi</span>
              {:else}
                <span class="durum">{r.status}</span>
              {/if}
              {#if r.admin_note}<div class="kucuk">{r.admin_note}</div>{/if}
            </td>
            <td>
              {#if r.status === "pending"}
                <button class="onay-btn" onclick={() => talepKarar(r.id, "approve")}>Onayla</button>
                <button class="red-btn" onclick={() => { redTalepId = r.id; redModalAcik = true; }}>Reddet</button>
              {:else}
                <span class="kucuk">-</span>
              {/if}
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="pagination">
    <button onclick={() => sayfaGit("istekler", -1)}
            disabled={(sayfalar.istekler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.istekler ?? 1} / {toplamSayfa(requests)}</span>
    <button onclick={() => sayfaGit("istekler", 1)}
            disabled={(sayfalar.istekler ?? 1) === toplamSayfa(requests)}>Sonraki</button>
  </div>
{:else}
  <p>Bu durumda istek yok.</p>
{/if}

<RedModal bind:acik={redModalAcik} onay={(sebep) => talepKarar(redTalepId, "reject", sebep)} />