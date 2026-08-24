# `AdminIstekler.svelte` Ayrıştırma Planı — Hazır

`App.svelte`'teki **"İstekler" sekmesini** `frontend/src/lib/AdminIstekler.svelte` dosyasına taşıma planını çıkardım. Dosya zaten oluşturulmuş ama boş — içeriği aşağıda. Plan `project_info__3.md` olarak kaydedildi.

> **Önemli:** Bu Explore Mode'dayım — kod yazamam. Uygulama için **Act Mode**'a geçin; aşağıdaki plan birebir uygulanabilir.

---

## Neden bu desen?

Daha önce bölünmüş 3 admin bileşeni aynı kalıbı kullanıyor:
- **Veri:** `let x = $state([])` ile yerel tutuyor
- **Yükleme:** `loadX()` + `Authorization: oturum.token` + `durum.error`'a yazma
- **Tetikleme:** `$effect(() => { if (durum.aktifSekme === "...") loadX(); })`
- **Üst bileşene çıkış:** `$props()` callback'leri (`onAcModal`, `onAcUrun`, ...)
- **Sayfalama:** store'daki `sayfala/toplamSayfa/sayfaGit` + `sayfalar.<sekme>` anahtarı
- **Modal:** o ekrana özgüyse bileşenin içine taşınmış (örn. `BayiStok` → `IslemModal`)

`AdminIstekler` de **aynı kalıba** uyacak.

---

## Tek kritik nokta: Toolbar rozeti

App.svelte toolbar'ında:
```svelte
İstekler {#if bekleyenSayi > 0}<span class="badge">{bekleyenSayi}</span>{/if}
```

`bekleyenSayi`, `requests` dizisinden türetiliyor ama **toolbar App'te kalıyor**. Çözüm: `requests`'i component-local değil, **paylaşımlı store'a** koy:

**`store.svelte.js`:**
```js
export const veri = $state({
  products: [],
  categories: [],
  myStock: [],
  requests: [],   // ← ekle
});
```

App'te:
```js
let bekleyenSayi = $derived(veri.requests.filter((r) => r.status === "pending").length);
```

`logout()` içine `veri.requests = [];` ekle (diğer `veri.*` gibi).

---

## 1. `AdminIstekler.svelte` tam içerik

```svelte
<script>
  import {
    API,
    oturum,
    durum,
    veri,
    jsonHeader,
    fiyatKolon,
    sayfalar,
    sayfala,
    toplamSayfa,
    sayfaGit,
  } from "./store.svelte.js";
  import RedModal from "./RedModal.svelte";

  let { onVeriDegisti } = $props();

  let redModalAcik = $state(false);
  let redTalepId = $state(null);
  let requestFiltre = $state("all");

  async function loadRequests(filtre = requestFiltre) {
    try {
      const res = await fetch(`${API}/api/requests?status=${filtre}`, {
        headers: { Authorization: oturum.token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      veri.requests = await res.json();
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
      onVeriDegisti?.();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function redModalAc(id) {
    redTalepId = id;
    redModalAcik = true;
  }

  $effect(() => {
    if (durum.aktifSekme === "istekler") {
      loadRequests(requestFiltre);
    }
  });
</script>

<h2>Bekleyen İstekler</h2>
<div class="filtre-satir">
  <button class:aktif={requestFiltre === "all"} onclick={() => (requestFiltre = "all")}>Hepsi</button>
  <button class:aktif={requestFiltre === "pending"} onclick={() => (requestFiltre = "pending")}>Bekleyen</button>
  <button class:aktif={requestFiltre === "approved"} onclick={() => (requestFiltre = "approved")}>Onaylanan</button>
  <button class:aktif={requestFiltre === "rejected"} onclick={() => (requestFiltre = "rejected")}>Reddedilen</button>
</div>

{#if veri.requests.length}
  <div class="tablo-cerceve">
    <table>
      <thead>
        <tr>
          <th>Bayi</th><th>Ürün</th><th>Eski Fiyat</th><th>Yeni Fiyat</th>
          <th>Aralık</th><th>Tarih</th><th>Durum</th><th>İşlem</th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(veri.requests, "istekler") as r}
          <tr>
            <td>{r.bayi}</td>
            <td>{r.urun}</td>
            <td>{fiyatKolon(r.old_price)}</td>
            <td><strong>{fiyatKolon(r.new_price)}</strong></td>
            <td class="kucuk">{fiyatKolon(r.alt_sinir)} - {fiyatKolon(r.ust_sinir)}</td>
            <td class="kucuk">{new Date(r.created_at).toLocaleString("tr-TR")}</td>
            <td>
              {#if r.status === "pending"}<span class="durum bekliyor">Bekliyor</span>
              {:else if r.status === "approved"}<span class="durum onayli">Onaylandı</span>
              {:else if r.status === "rejected"}<span class="durum redli">Reddedildi</span>
              {:else}<span class="durumlar">{r.status}</span>{/if}
              {#if r.admin_note}<div class="kucuk">{r.admin_note}</div>{/if}
            </td>
            <td>
              {#if r.status === "pending"}
                <button class="onay-btn" onclick={() => talepKarar(r.id, "approve")}>Onayla</button>
                <button class="red-btn" onclick={() => redModalAc(r.id)}>Reddet</button>
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
    <button onclick={() => sayfaGit("istekler", -1)} disabled={(sayfalar.istekler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.istekler ?? 1} / {toplamSayfa(veri.requests)}</span>
    <button onclick={() => sayfaGit("istekler", 1)} disabled={(sayfalar.istekler ?? 1) === toplamSayfa(veri.requests)}>Sonraki</button>
  </div>
{:else}
  <p>Bu durumda istek yok.</p>
{/if}

<RedModal
  bind:acik={redModalAcik}
  onay={(sebep) => talepKarar(redTalepId, "reject", sebep)}
/>
```

---

## 2. `App.svelte` değişiklikleri

**Sil:**
- `let requests = $state([]);`
- `let requestFiltre = $state("all");`
- `let redModalAcik = $state(false);`
- `let redTalepId = $state(null);`
- `loadRequests()`, `talepKarar()`, `redModalAc()` fonksiyonları
- istekler `$effect` bloğu
- `import RedModal from "./lib/RedModal.svelte";` ve alttaki `<RedModal ... />`

**Değiştir:**
- `let bekleyenSayi = $derived(requests.filter(...))` → `let bekleyenSayi = $derived(veri.requests.filter(...))`

**Ekle:**
- `import AdminIstekler from "./lib/AdminIstekler.svelte";`
- `{:else if durum.aktifSekme === "istekler"}` bloğu → `<AdminIstekler onVeriDegisti={loadAll} />`
- `logout()` içinde `veri.requests = [];`

---

## 3. `store.svelte.js` değişikliği

```js
export const veri = $state({
  products: [],
  categories: [],
  myStock: [],
  requests: [],   // ← tek satır
});
```

---

## Doğrulama listesi

- [ ] "İstekler" sekmesi tabloyu gösteriyor
- [ ] Filtre butonları (Hepsi/Bekleyen/Onaylanan/Reddedilen) listeyi yeniliyor
- [ ] Sayfalama çalışıyor (`sayfalar.istekler`)
- [ ] Onayla → talep approved görünüyor, ürünler/kategoriler de güncelleniyor
- [ ] Reddet → modal açılıyor, sebep girilince reject gidiyor
- [ ] Toolbar rozetinde bekleyen sayısı görünüyor
- [ ] Logout sonrası `veri.requests` temizleniyor

Plan ayrıntılı olarak `project_info__3.md` dosyasına kaydedildi. **Act Mode'a geçince** bu planı birebir uygulayabilirsiniz.