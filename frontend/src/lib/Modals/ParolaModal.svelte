<script>
  import { API, oturum, jsonHeader } from "../store.svelte.js";

  let eski = $state("");
  let yeni = $state("");
  let yeni2 = $state("");
  let hata = $state("");
  let yukleniyor = $state(false);

  async function kaydet() {
    hata = "";
    if (yeni !== yeni2) {
      hata = "Yeni şifreler eşleşmiyor";
      return;
    }
    yukleniyor = true;
    try {
      const res = await fetch(`${API}/api/change-password`, {
        method: "POST",
        headers: jsonHeader(),
        body: JSON.stringify({ eski_sifre: eski, yeni_sifre: yeni }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      oturum.sifreDegistir = false;
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    } finally {
      yukleniyor = false;
    }
  }
</script>

<div class="modal-arkaplan" role="presentation">
  <div class="modal" role="presentation">
    <h3>Şifre Değiştir</h3>
    <p class="modal-bilgi">
      Güvenlik için varsayılan şifrenizi değiştirmelisiniz. Devam etmeden önce
      yeni bir şifre belirleyin.
    </p>

    <label>Mevcut Şifre
      <input type="password" bind:value={eski} placeholder="Mevcut şifreniz" />
    </label>

    <label>Yeni Şifre
      <input type="password" bind:value={yeni}
             placeholder="En az 8 karakter, harf ve rakam"
             onkeydown={(e) => e.key === "Enter" && kaydet()} />
    </label>

    <label>Yeni Şifre (Tekrar)
      <input type="password" bind:value={yeni2}
             placeholder="Yeni şifre tekrar"
             onkeydown={(e) => e.key === "Enter" && kaydet()} />
    </label>

    {#if hata}<p class="error">{hata}</p>{/if}

    <div class="modal-butonlar">
      <button class="ekle-btn" onclick={kaydet} disabled={yukleniyor}>
        {yukleniyor ? "Kaydediliyor..." : "Kaydet"}
      </button>
    </div>
  </div>
</div>
