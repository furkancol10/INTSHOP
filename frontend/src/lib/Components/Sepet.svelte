<script>
    import { onMount } from "svelte";
    import {
        API,
        authHeader,
        jsonHeader,
        durum,
        fiyatKolon,
        sepet,
        sepetYukle,
    } from "../store.svelte.js";
    import OnayModal from "../Modals/OnayModal.svelte";

    let gruplar = $derived(
        Object.values(
            sepet.satirlar.reduce((acc, s) => {
                acc[s.dealer_id] ??= {
                    bayi: s.bayi,
                    satirlar: [],
                    araToplam: 0,
                };
                acc[s.dealer_id].satirlar.push(s);
                acc[s.dealer_id].araToplam += Number(s.satir_tutar);
                return acc;
            }, {}),
        ),
    );

    async function adetDegistir(id, yeni) {
        try {
            const res = await fetch(`${API}/api/cart/${id}`, {
                method: "PUT",
                headers: jsonHeader(),
                body: JSON.stringify({ quantity: yeni }),
            });
            if (!res.ok) throw new Error(await res.text());
            await sepetYukle();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    async function satirSil(id) {
        try {
            const res = await fetch(`${API}/api/cart/${id}`, {
                method: "DELETE",
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(await res.text());
            await sepetYukle();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    let onayAcik = $state(false);

    async function sepetiBosalt() {
        try {
            const res = await fetch(`${API}/api/cart`, {
                method: "DELETE",
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(await res.text());
            await sepetYukle();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    let oneriler = $state([]);

    async function onerileriYukle() {
        try {
            const res = await fetch(`${API}/api/shop?limit=12`, {
                headers: authHeader(),
            });
            if (!res.ok) return;
            const tumu = await res.json();
            const sepettekiIdler = new Set(sepet.satirlar.map((s) => s.product_id));
            oneriler = tumu.filter((u) => !sepettekiIdler.has(u.product_id)).slice(0, 6);
        } catch {
            // öneriler yüklenemezse sessizce yok say
        }
    }

    function urunAc(productId) {
        durum.secilenUrunId = productId;
        durum.aktifSekme = "urun-detay";
    }

    onMount(async () => {
        await sepetYukle();
        await onerileriYukle();
    });
</script>

<div class="flex justify-between items-center pb-4 border-b border-slate-100 mb-5">
    <h2 class="text-2xl font-semibold">Sepetim <span class="text-[0.95rem] text-gray-500 font-normal">({sepet.adet} ürün)</span></h2>
    {#if sepet.satirlar.length}
        <button class="bg-orange-600 border-none text-white cursor-pointer text-[0.9rem] rounded-[7px]" onclick={() => (onayAcik = true)}
            >Sepeti Temizle</button
        >
    {/if}
</div>

{#if sepet.satirlar.length}
    <div class="grid grid-cols-1 md:grid-cols-[1fr_300px] gap-5 items-start">
        <div>
            {#each gruplar as g}
                <div class="bg-slate-100 border border-orange-300 rounded-[10px] mb-4 overflow-hidden">
                    <div class="flex justify-between px-4 py-[0.8rem] bg-slate-100 border-b border-slate-100 text-[0.9rem]">
                        <span>Satıcı: <strong>{g.bayi}</strong></span>
                        <span class="text-orange-600 font-semibold">{fiyatKolon(g.araToplam)}</span
                        >
                    </div>

                    {#each g.satirlar as s}
                        <div class="grid grid-cols-[60px_1fr] md:grid-cols-[70px_1fr_auto_auto] gap-x-4 gap-y-[0.6rem] md:gap-4 items-center p-4 border-b border-slate-100 last:border-b-0">
                            <div>
                                {#if s.image_url}
                                    <img
                                        src={s.image_url}
                                        alt={s.urun}
                                        class="w-[70px] object-contain border border-slate-100 rounded-md"
                                        onerror={(e) =>
                                            (e.currentTarget.src =
                                                "/images/placeholder.png")}
                                    />
                                {:else}
                                    <div class="w-[70px] h-[70px] grid place-items-center bg-slate-100 rounded-md text-slate-100">-</div>
                                {/if}
                            </div>

                            <div class="flex flex-col gap-1">
                                <span class="text-[0.95rem]">{s.urun}</span>
                                <span class="text-[0.82rem] text-gray-500"
                                    >{fiyatKolon(s.price)} / adet</span
                                >
                                {#if s.quantity > s.stock}
                                    <span class="text-[0.78rem] text-red-600"
                                        >Stok {s.stock} adete düştü</span
                                    >
                                {/if}
                            </div>

                            <div class="inline-flex items-center gap-[0.4rem] border border-slate-100 rounded-[20px] px-[0.35rem] py-[0.2rem]">
                                {#if s.quantity <= 1}
                                    <button
                                        class="w-[26px] h-[26px] border-none bg-transparent cursor-pointer text-orange-600 text-base rounded-full enabled:hover:bg-slate-100 disabled:opacity-30 disabled:cursor-default"
                                        onclick={() => satirSil(s.id)}
                                        title="Kaldır">🗑</button
                                    >
                                {:else}
                                    <button
                                        class="w-[26px] h-[26px] border-none bg-transparent cursor-pointer text-orange-600 text-base rounded-full enabled:hover:bg-slate-100 disabled:opacity-30 disabled:cursor-default"
                                        onclick={() =>
                                            adetDegistir(s.id, s.quantity - 1)}
                                        >-</button
                                    >
                                {/if}
                                <span class="min-w-[20px] text-center text-[0.9rem]">{s.quantity}</span>
                                <button
                                    class="w-[26px] h-[26px] border-none bg-transparent cursor-pointer text-orange-600 text-base rounded-full enabled:hover:bg-slate-100 disabled:opacity-30 disabled:cursor-default"
                                    onclick={() =>
                                        adetDegistir(s.id, s.quantity + 1)}
                                    disabled={s.quantity >= s.stock}>+</button
                                >
                            </div>

                            <div class="font-semibold whitespace-nowrap">
                                {fiyatKolon(s.satir_tutar)}
                            </div>
                        </div>
                    {/each}
                </div>
            {/each}
        </div>

        <aside class="bg-white border border-teal-600 rounded-[10px] p-5 flex flex-col gap-[0.6rem] static md:sticky md:top-4">
            <span class="text-[0.75rem] tracking-[0.04rem] text-orange-600">SEÇİLEN ÜRÜNLER ({sepet.adet})</span>
            <span class="text-[1.6rem] font-bold">{fiyatKolon(sepet.toplam)}</span>
            <!--                            Sipariş Sistemi HENÜZ Yok                           -->

            <button class="p-[0.8rem] border-none rounded-lg bg-orange-600 text-white text-[0.95rem] cursor-pointer disabled:bg-slate-100 disabled:text-gray-500 disabled:cursor-not-allowed" disabled>Alışverişi Tamamla</button>
            <p class="text-[0.78rem] text-gray-500 text-center m-0">Sipariş oluşturma geliştirme aşamasında!</p>
        </aside>
    </div>
{:else}
    <div class="p-12 text-center text-gray-500">
        <p>Sepetiniz boş.</p>
    </div>
{/if}

{#if oneriler.length}
    <div class="mt-10 pt-6 border-t border-slate-100">
        <h3 class="m-0 mb-4">Önerilen Ürünler</h3>
        <div class="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-6">
            {#each oneriler as urun}
                <button class="border border-slate-100 rounded-[18px] p-4 flex flex-col gap-2 bg-white text-left cursor-pointer transition hover:border-teal-600 hover:shadow-[0_4px_14px_rgba(0,0,0,0.08)] hover:-translate-y-0.5 active:scale-[0.98]" onclick={() => urunAc(urun.product_id)}>
                    {#if urun.image_url}
                        <img
                            src={urun.image_url}
                            alt={urun.name}
                            class="w-full h-40 object-contain rounded-lg"
                            onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
                        />
                    {/if}
                    <h3 class="m-0 mb-4">{urun.name}</h3>
                    <p class="m-0 text-sm text-orange-600">
                        En uygun: <strong>{urun.dealer_name}</strong>
                    </p>
                    <p class="text-[1.3rem] font-bold text-blue-950 m-0">{fiyatKolon(urun.price)}</p>
                </button>
            {/each}
        </div>
    </div>
{/if}

<OnayModal
    bind:acik={onayAcik}
    baslik="Sepeti Temizle"
    mesaj="Sepetinizdeki tüm ürünler kaldırılacak. Devam etmek istiyor musunuz?"
    onayYazi="Boşalt"
    onaylandi={sepetiBosalt}
/>
