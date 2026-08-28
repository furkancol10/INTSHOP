<script>
  import { API, veri, authHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";
  import { ozellikAlanlari, ozellikleriAyristir } from "../urunOzellikleri.js";
  import UrunModal from "../Modals/UrunModal.svelte";

  let { yenile } = $props();

  let modalAcik = $state(false);
  let duzenlenenUrun = $state(null);

  function ozellikOzeti(p) {
    const alanlar = ozellikAlanlari(p.category);
    if (!alanlar.length) return "-";
    const degerler = ozellikleriAyristir(p.attributes);
    const dolu = alanlar
      .filter((a) => degerler[a.key])
      .map((a) => `${a.label}: ${degerler[a.key]}`);
    return dolu.length ? dolu.join(" · ") : "-";
  }

  function ekleAc() {
    duzenlenenUrun = null;
    modalAcik = true;
  }

  function duzenleAc(p) {
    duzenlenenUrun = p;
    modalAcik = true;
  }

  async function sil(id) {
    try {
      const res = await fetch(`${API}/api/products/${id}`, {
        method: "DELETE",
        headers: authHeader(),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      yenile();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }
</script>

<h2>Ürünler</h2>
<button class="ekle-btn" onclick={ekleAc}>+ Yeni Ürün</button>

{#if veri.products.length}
  <div class="tablo-cerceve">
    <table>
      <thead>
        <tr>
          <th>ID</th><th>Resim</th><th>Ürün</th><th>Kategori</th>
          <th>Özellikler</th>
          <th>Toplam Stok</th><th>Fiyat</th><th>Aralık</th><th></th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(veri.products, "urunler") as p}
          <tr>
            <td>{p.id}</td>
            <td>{#if p.image_url}<img src={p.image_url} alt={p.name} />{/if}</td>
            <td>{p.name}</td>
            <td>{p.parent_category ? `${p.parent_category} › ${p.category}` : p.category}</td>
            <td class="kucuk ozellik-hucre">{ozellikOzeti(p)}</td>
            <td>{p.toplam_stok}</td>
            <td>{fiyatKolon(p.price)}</td>
            <td class="kucuk">{fiyatKolon(p.alt_sinir)} - {fiyatKolon(p.ust_sinir)}</td>
            <td>
              <button class="duzenle-btn" onclick={() => duzenleAc(p)}>Düzenle</button>
              <button class="sil" onclick={() => sil(p.id)}>Sil</button>
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="pagination">
    <button onclick={() => sayfaGit("urunler", -1)}
            disabled={(sayfalar.urunler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.urunler ?? 1} / {toplamSayfa(veri.products)}</span>
    <button onclick={() => sayfaGit("urunler", 1)}
            disabled={(sayfalar.urunler ?? 1) === toplamSayfa(veri.products)}>Sonraki</button>
  </div>
{/if}

<UrunModal bind:acik={modalAcik} urun={duzenlenenUrun} kaydedildi={yenile} />