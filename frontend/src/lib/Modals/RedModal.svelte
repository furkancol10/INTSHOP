<script>
  let { acik = $bindable(), onay } = $props();
  let sebep = $state("");

  function kapat() {
    acik = false;
    sebep = "";
  }

  function gonder() {
    onay(sebep);
    kapat();
  }
</script>

{#if acik}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="bg-white p-8 rounded-xl w-80 flex flex-col gap-[.8rem] shadow-[0_8px_32px_rgba(255,255,255,0.2)]" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3 class="m-0 mb-2">Talebi Reddet</h3>
      <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Red Sebebi
        <input class="font-normal" bind:value={sebep} placeholder="Örn: Fiyat çok düşük"
               onkeydown={(e) => e.key === "Enter" && gonder()} />
      </label>
      <div class="flex gap-2 justify-end mt-2">
        <button class="bg-white border-0 px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>İptal</button>
        <button class="bg-red-600 text-white border-0 py-[.35rem] px-[.7rem] rounded-[5px] cursor-pointer" onclick={gonder}>Reddet</button>
      </div>
    </div>
  </div>
{/if}