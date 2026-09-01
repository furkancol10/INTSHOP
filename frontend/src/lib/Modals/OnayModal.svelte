<script>
    let {
        acik = $bindable(false),
        baslik = "Emin misiniz ?",
        mesaj = "",
        onayYazi = "Evet",
        iptalYazi = "Vazgeç",
        onaylandi,
    } = $props();

    function kapat() {
        acik = false;
    }

    function onayla() {
        acik = false;
        onaylandi?.();
    }
</script>

{#if acik}
    <div class="fixed inset-0 bg-black/45 grid place-items-center z-[1000]" onclick={kapat} role="presentation">
        <div class="bg-white rounded-xl p-6 min-w-[320px] shadow-[0_10px_40px_rgba(0,0,0,0.2)] max-w-[380px]" onclick={(e) => e.stopPropagation()} role="presentation">
            <h3>{baslik}</h3>
            {#if mesaj}<p class="text-slate-600 text-[.92rem] leading-[1.5]">{mesaj}</p>{/if}
            <div class="flex gap-[.6rem] justify-end mt-[1.2rem]">
                <button class="px-[1.1rem] py-[.55rem] border border-gray-300 rounded-lg bg-white cursor-pointer" onclick={kapat}>{iptalYazi}</button>
                <button class="px-[1.1rem] py-[.55rem] rounded-lg bg-red-700 text-white border-none cursor-pointer" onclick={onayla}>{onayYazi}</button>
            </div>
        </div>
    </div>
{/if}