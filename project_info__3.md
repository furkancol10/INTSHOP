# stok-panel — AdminIstekler.svelte Ayrıştırma Planı (App.svelte → Bileşen)

## Görev Özeti

`App.svelte` içindeki **"istekler" sekmesi** (admin fiyat talepleri ekranı) `frontend/src/lib/AdminIstekler.svelte` dosyasına taşınacak. Dosya zaten oluşturulmuş ama **boş**. Hedef: daha önce ayrıştırılmış `AdminHareketler.svelte`, `AdminUrunler.svelte`, `AdminKategoriler.svelte` ile **birebir aynı deseni** kullanmak.

> **Not:** Bu rapor Explore Mode'da hazırlandı; kod yazma yetkisi yok. Uygulama için Act Mode'a geçilip aşağıdaki plan birebir uygulanmalı.

---

## 1. Tespit Edilen Ayrıştırma Deseni (mevcut bileşenlerden)

Üç tamamlanmış bileşende ortak kalıp:

| Öğe | Desen |
|---|---|
| Veri | Bileşen kendi yerel `let x = $state([])` tutar |
| Yükleme | Bileşen içinde `async function loadX()` — `fetch` + `Authorization: oturum.token` + `durum.error`'a hata yazma |
| Tetikleme | `$effect(() => { if (durum.aktifSekme === "<sekme>") loadX(); })` |
| Üst bileşene çıkış | `let { onAcBirsey } = $props()` callback prop'ları (AdminKategoriler: `onAcDetay`, `onAcModal`; AdminUrunler: `onAcUrun`) |
| Sayfalama | Global `sayfalar` store'u, sekmeye özel anahtar (`"hareketler"`, `"urunler"`…) + `sayfala/toplamSayfa/sayfaGit` |
| Modal sahipliği | Modal yalnızca o ekranda kullanılıyorsa bileşenin içine taşınır (örn. `BayiStok` → `IslemModal`) |
| Stil | Scoped style yok; tümü global `app.css` sınıfları |

`AdminIstekler` bu kalıba uyar; **tek farkı** toolbar'daki rozet (bkz. Bölüm 4).

---

## 2. Taşınacak Kod Envanteri (App.svelte'den)

### State'ler
```js
let requests = $state([]);            // → AdminIstekler (veya store, bkz. §4A)
let requestFiltre = $state("all");    // → AdminIstekler
let redModalAcik = $state(false);     // → AdminIstekler
let redTalepId = $state(null);        // → AdminIstekler
let bekleyenSayi = $derived(...)       // App'te KALMAK ZORUNDA (toolbar rozeti) — kaynağı değişir, §4
```

### Fonksiyonlar
```js
async function loadRequests(filtre = requestFiltre) { ... }   // → AdminIstekler
async function talepKarar(id, karar, not = "") { ... }        // → AdminIstekler (loadAll çağrısı callback'e döner, §4B)
function redModalAc(id) { ... }                               // → AdminIstekler
$effect(() => { if (oturum.role === "Admin" && durum.aktifSekme === "istekler") loadRequests(requestFiltre); })  // → AdminIstekler (role kontrolü düşer)
```

### Şablon bloğu
`{:else if durum.aktifSekme === "istekler"}` altındaki her şey: başlık, `.filtre-satir` filtre butonları, tablo (Bayi/Ürün/Eski Fiyat/Yeni Fiyat/Aralık/Tarih/Durum/İşlem), `.pagination`, boş durum mesajı `"Bu durumda istek yok."`.

### Modal
`<RedModal>` **tamamen** AdminIstekler'e taşınır (yalnızca istek ekranında kullanılıyor). App.svelte'den import + mount + `redModalAcik`/`redTalepId` silinir.

---

## 3. Önerilen `AdminIstekler.svelte` İçeriği (tam dosya)

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

  // Svelte 5 Props
  let { onVeriDegisti } = $props();

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

  let redModalAcik = $state(false);
  let redTalepId = $state(null);
  let requestFiltre = $state("all");

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

*(Düzeltme: `let redModalAcik/redTalepId/requestFiltre` bildirimleri fonksiyonlardan önce, script başında olmalı — yukarıdaki sıralamayı uygularken state bildirimlerini en üste alın.)*

---

## 4. Kesişen Bağlantılar ve Çözümleri

### 4A. Toolbar rozeti (`bekleyenSayi`) — kritik nokta

App.svelte toolbar'ında:
```svelte
İstekler {#if bekleyenSayi > 0}<span class="badge">{bekleyenSayi}</span>{/if}
```
Bu rozet `requests` dizisinden türetiliyor; toolbar App'te kalıyor. **Çözüm (önerilen):** `requests`'i bileşen-local değil, paylaşımlı store'a koy:

**`store.svelte.js`:**
```js
export const veri = $state({
  products: [],
  categories: [],
  myStock: [],
  requests: [],        // ← EKLE
});
```

- `AdminIstekler` → `veri.requests = await res.json()` (yukarıdaki dosyada böyle)
- `App.svelte` → `let bekleyenSayi = $derived(veri.requests.filter((r) => r.status === "pending").length);` (değişmez, sadece kaynağı store olur)
- `logout()` içine `veri.requests = [];` satırını **ekle** (products/myStock/categories gibi).

**Alternatif:** `onBekleyenDegisti(n)` callback prop'u ile App'te `let bekleyenSayi = $state(0)` tutmak. Çalışır ama projedeki `veri.*` paylaşım alışkanlığına daha az uygun; önerilmez.

> Bilinen sınırlama (mevcut davranış, korunuyor): istekler sekmesi açılana kadar liste yüklenmediği için rozet sayısı bayi yeni talep gönderse bile eski kalır. Canlı rozet istenirse ayrıca periyodik/push yükleme gerekir — bu görevin kapsamı dışı.

### 4B. `talepKarar` sonrası `loadAll()` bağımlılığı

Orijinal kod onay/red sonrası `loadRequests()` + `loadAll()` çağırıyordu (onaylanan fiyat değişikliği ürün/stok verilerini etkiliyor). `loadAll` App'te kalıyor; bileşen bunu **callback ile** çağırır:

**App.svelte bağlantısı:**
```svelte
{:else if durum.aktifSekme === "istekler"}
  <AdminIstekler onVeriDegisti={loadAll} />
```

Bu, AdminUrunler'in `onAcUrun` callback'iyle aynı haberleşme yönüdür (çocuk → ebeveyn olayı).

### 4C. Effect içindeki rol kontrolü

Orijinal effect `oturum.role === "Admin"` kontrolü içeriyordu çünkü App'in global scope'undaydı. Bileşene taşınınca gereksiz — bileşen yalnızca admin'e görünen sekmede render ediliyor. `AdminHareketler`/`AdminUrunler` deseninde olduğu gibi **yalnızca sekme kontrolü** bırak.

### 4D. Header seçimi

`talepKarar` orijinalde inline `{ "Content-Type": ..., Authorization: ... }` kullanıyordu; store'daki `jsonHeader()` birebir aynı şeyi yapıyor. `jsonHeader()` tercih edildi (BayiStok'un `authHeader()` kullanımıyla tutarlı).

---

## 5. App.svelte'de Yapılacak Silmeler/Düzeltmeler

1. **Sil:** `let requests = $state([]);`, `let requestFiltre = $state("all");`, `let redModalAcik`, `let redTalepId`
2. **Değiştir:** `let bekleyenSayi = $derived(requests.filter(...))` → `let bekleyenSayi = $derived(veri.requests.filter(...))`
3. **Sil:** `loadRequests`, `talepKarar`, `redModalAc` fonksiyonları ve istekler `$effect`'i
4. **Sil:** `import RedModal from "./lib/RedModal.svelte";` ve en alttaki `<RedModal ... />` mount'u
5. **Ekle:** `import AdminIstekler from "./lib/AdminIstekler.svelte";`
6. **Değiştir:** `{:else if durum.aktifSekme === "istekler"}` bloğunun tamamı → `<AdminIstekler onVeriDegisti={loadAll} />`
7. **Ekle:** `logout()` içine `veri.requests = [];`

## 6. store.svelte.js Değişikliği

```js
export const veri = $state({
  products: [],
  categories: [],
  myStock: [],
  requests: [],   // ← tek satır ekleme
});
```

---

## 7. Doğrulama Kontrol Listesi

- [ ] Admin girişi → "İstekler" sekmesi: tablo + filtre butonları çalışıyor
- [ ] Filtre değişimi (Hepsi/Bekleyen/...) listeyi yeniden yüklüyor ($effect `requestFiltre`'i okuduğu için otomatik)
- [ ] Sayfalama Önceki/Sonraki çalışıyor (`sayfalar.istekler` anahtarı korundu)
- [ ] "Onayla" → talep approved oluyor, liste yenileniyor, **ürünler/kategoriler de yenileniyor** (onVeriDegisti=loadAll)
- [ ] "Reddet" → RedModal açılıyor, sebep girilince reject gönderiliyor, modal kapanıyor
- [ ] Bekleyen talep varken toolbar rozetinde sayı görünüyor; onay/red sonrası güncelleniyor
- [ ] Logout sonrası tekrar girişte eski istek listesi sızmıyor (`veri.requests = []`)
- [ ] Bayi/Kullanıcı rollerinde hiçbir regresyon yok (dokunulan tek admin sekmesi)

## 8. Edit Sırasında Görülen Ek Temizlik Fırsatları (opsiyonel)

- `App.svelte`'te `islemModalAcik` / `islemUrunId` state'leri artık ölü (BayiStok kendi kopyalarına sahip) — silinebilir.
- `App.svelte`'teki `import { Chart, registerables }` + `Chart.register(...registerables)` muhtemelen ölü (grafik BayiRaporlar'a taşındı); BayiRaporlar kendi importunu yapıyorsa App'ten kaldır.
- `App.svelte.yedek` dosyası repo'da duruyor — silinebilir.

---

*Bu plan `project_info__3.md` olarak kaydedildi. Uygulamak için Act Mode'a geçin; plan birebir uygulanabilir durumda.*
