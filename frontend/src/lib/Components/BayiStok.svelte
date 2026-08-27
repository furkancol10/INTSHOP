<script>
  import { onMount } from "svelte";
  import { API, veri, authHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";
  import IslemModal from "../Modals/IslemModal.svelte"

  let islemModalAcik = $state(false);
  let islemUrunId = $state("");

  async function loadMyStock() {
    try {
      const res = await fetch(`${API}/api/my-stock`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      veri.myStock = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function islemModalAc(productId = "") {
    islemUrunId = productId;
    islemModalAcik = true;
  }

  function depoDurumu(stok) {
    const s = Number(stok) || 0;
    const yuzde = Math.min(100, Math.round((s / 100) * 100));
    let sinif = "bol";
    let etiket = "Bol";
    if (s === 0) { sinif = "bos"; etiket = "Tükendi"; }
    else if (s <= 10) { sinif = "kritik"; etiket = "Kritik"; }
    else if (s <= 30) { sinif = "az"; etiket = "Az"; }
    else if (s <= 60) { sinif = "normal"; etiket = "Normal"; }
    return { yuzde, sinif, sayi: s, etiket };
  }

  onMount(loadMyStock);
</script>

<h2>Stok Yönetimi</h2>
<div class="tablo-cerceve">
  <button class="islem-btn1" onclick={() => islemModalAc("")}>Stok/fiyat</button>
  <table>
    <thead>
      <tr>
        <th>Ürün-Id</th><th>Ürün</th><th>Bayi Fiyatı</th><th>Kategori</th>
        <th>Depo Durumu</th><th>Son Güncelleme Tarihi</th><th>İşlem</th>
      </tr>
    </thead>
    <tbody>
      {#each sayfala(veri.myStock, "stok") as p}
        {@const d = depoDurumu(p.stock)}
        <tr class:dusuk={p.stock < 10}>
          <td>{p.product_id}</td>
          <td>{p.name}</td>
          <td>{fiyatKolon(p.benim_fiyatim)}</td>
          <td>{p.category}</td>
          <td>
            <div class="depo-bar">
              <div class="depo-dolu {d.sinif}" style="width: {d.yuzde}%"></div>
              <span class="depo-yazi">{d.sayi}</span>
            </div>
            <span class="depo-etiket {d.sinif}">{d.etiket}</span>
          </td>
          <td class="tarih-hucre">
            {p.son_hareket ? new Date(p.son_hareket).toLocaleString("tr-TR") : "-"}
          </td>
          <td>
            <button class="islem-btn" onclick={() => islemModalAc(p.product_id)}>Stok/fiyat</button>
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
</div>
<div class="pagination">
  <button onclick={() => sayfaGit("stok", -1)}
          disabled={(sayfalar.stok ?? 1) === 1}>Önceki</button>
  <span>Sayfa {sayfalar.stok ?? 1} / {toplamSayfa(veri.myStock)}</span>
  <button onclick={() => sayfaGit("stok", 1)}
          disabled={(sayfalar.stok ?? 1) === toplamSayfa(veri.myStock)}>Sonraki</button>
</div>

<IslemModal
  bind:acik={islemModalAcik}
  urunId={islemUrunId}
  tamamlandi={(tur) => {
    loadMyStock();
    if (tur === "fiyat") {
      durum.bildirim = "Fiyat talebiniz onaya gönderildi.";
      setTimeout(() => (durum.bildirim = ""), 4000);
    }
  }}
/>