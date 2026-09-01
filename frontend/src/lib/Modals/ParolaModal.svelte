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

<div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" role="presentation">
  <div class="bg-white p-8 rounded-xl w-80 flex flex-col gap-[.8rem] shadow-[0_8px_32px_rgba(255,255,255,0.2)]" role="presentation">
    <h3 class="m-0 mb-2">Şifre Değiştir</h3>
    <p class="m-0 text-sm bg-slate-100 text-gray-500 text-left">
      Güvenlik için varsayılan şifrenizi değiştirmelisiniz. Devam etmeden önce
      yeni bir şifre belirleyin.
    </p>

    <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Mevcut Şifre
      <input class="font-normal" type="password" bind:value={eski} placeholder="Mevcut şifreniz" />
    </label>

    <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Yeni Şifre
      <input class="font-normal" type="password" bind:value={yeni}
             placeholder="En az 8 karakter, harf ve rakam"
             onkeydown={(e) => e.key === "Enter" && kaydet()} />
    </label>

    <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Yeni Şifre (Tekrar)
      <input class="font-normal" type="password" bind:value={yeni2}
             placeholder="Yeni şifre tekrar"
             onkeydown={(e) => e.key === "Enter" && kaydet()} />
    </label>

    {#if hata}<p class="text-red-700 text-[.85rem] m-0">{hata}</p>{/if}

    <div class="flex gap-2 justify-end mt-2">
      <button class="bg-teal-600 text-white border-0 px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={kaydet} disabled={yukleniyor}>
        {yukleniyor ? "Kaydediliyor..." : "Kaydet"}
      </button>
    </div>
  </div>
</div>
