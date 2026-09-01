<script>
  import { durum, sepet, fiyatKolon } from "../store.svelte.js";

  function ac() {
    durum.sepetPopup = !durum.sepetPopup;
  }

  function kapat() {
    durum.sepetPopup = false;
  }

  function sepeteGit() {
    durum.aktifSekme = "sepet";
    durum.sepetPopup = false;
  }
</script>

<div class="fixed top-[72px] right-8 z-[260]">
  <button class="relative w-12 h-12 rounded-full border-none bg-teal-600 flex items-center justify-center cursor-pointer shadow-[0_4px_14px_rgba(0,0,0,0.2)] transition-all duration-200 hover:bg-teal-700 active:scale-[0.94]" onclick={ac} aria-label="Sepet">
    <svg viewBox="0 0 24 24" width="22" height="22" fill="white">
      <path
        d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12L8.1 13h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A.996.996 0 0 0 20 4H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"
      />
    </svg>
    {#if sepet.adet > 0}
      <span class="absolute -top-1 -right-1 bg-orange-600 text-white text-[0.7rem] font-bold min-w-[18px] h-[18px] px-1 rounded-[9px] flex items-center justify-center border-2 border-white">{sepet.adet}</span>
    {/if}
  </button>

  {#if durum.sepetPopup}
    <div
      class="fixed inset-0 z-[259]"
      onclick={kapat}
      role="presentation"
    ></div>
    <div class="sepet-widget-popup">
      <h4>Sepetim</h4>
      {#if sepet.satirlar.length}
        <div class="flex flex-col gap-[0.6rem] max-h-[260px] overflow-y-auto">
          {#each sepet.satirlar as s}
            <div class="grid grid-cols-[40px_1fr_auto] gap-[0.6rem] items-center">
              {#if s.image_url}
                <img
                  src={s.image_url}
                  alt={s.urun}
                  class="w-10 h-10 object-contain border border-slate-100 rounded-md bg-slate-100"
                  onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
                />
              {:else}
                <div class="w-10 h-10 object-contain border border-slate-100 rounded-md bg-slate-100"></div>
              {/if}
              <div class="flex flex-col gap-[0.1rem] min-w-0">
                <span class="text-[0.85rem] whitespace-nowrap overflow-hidden text-ellipsis">{s.urun}</span>
                <span class="text-[0.75rem] text-gray-500">{s.quantity} adet</span>
              </div>
              <span class="text-[0.85rem] font-semibold whitespace-nowrap">{fiyatKolon(s.satir_tutar)}</span>
            </div>
          {/each}
        </div>
        <div class="flex justify-between items-center mt-[0.8rem] pt-[0.7rem] border-t border-slate-100 text-[0.95rem]">
          <span>Toplam</span>
          <strong>{fiyatKolon(sepet.toplam)}</strong>
        </div>
        <button class="w-full mt-[0.7rem] p-[0.6rem] bg-orange-600 text-white border-none rounded-lg text-[0.9rem] cursor-pointer hover:bg-orange-700" onclick={sepeteGit}>Sepete Git</button>
      {:else}
        <p class="m-0 text-gray-500 text-[0.85rem]">Sepetiniz boş.</p>
      {/if}
    </div>
  {/if}
</div>
