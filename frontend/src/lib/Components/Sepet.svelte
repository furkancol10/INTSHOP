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
            sepet.satirlar.reduce((acc,s) => {
                acc[s.dealer_id] ??= { bayi: s.bayi, satirlar: [], araToplam: 0 };
                acc[s.dealer_id].satirlar.push(s);
                acc[s.dealer_id].araToplam += Number(s.satir_tutar);
                return acc;
            }, {})
        )
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

    onMount(sepetYukle);
</script>

<div class="sepet-baslik">
    <h2>Sepetim <span class="sepet-sayi">({sepet.adet} ürün)</span></h2>
    {#if sepet.satirlar.length}
        <button class="sepet-bosalt" onclick={() => (onayAcik = true)}
            >Sepeti Temizle</button
        >
    {/if}
</div>

{#if sepet.satirlar.length}
    <div class="sepet-duzen">
        <div class="sepet-sol">
            {#each gruplar as g}
                <div class="bayi-karti">
                    <div class="bayi-basligi">
                        <span>Satıcı: <strong>{g.bayi}</strong></span>
                        <span class="ara-toplam">{fiyatKolon(g.araToplam)}</span
                        >
                    </div>

                    {#each g.satirlar as s}
                        <div class="sepet-satir">
                            <div class="satir-gorsel">
                                {#if s.image_url}
                                    <img src={s.image_url} alt={s.urun} />
                                {:else}
                                    <div class="gorsel-yok">-</div>
                                {/if}
                            </div>

                            <div class="satir-bilgi">
                                <span class="satir-ad">{s.urun}</span>
                                <span class="satir-birim"
                                    >{fiyatKolon(s.price)} / adet</span
                                >
                                {#if s.quantity > s.stock}
                                    <span class="stok-uyari"
                                        >Stok {s.stock} adete düştü</span
                                    >
                                {/if}
                            </div>

                            <div class="adet-kontrol">
                                {#if s.quantity <= 1}
                                    <button
                                        class="adet-sil"
                                        onclick={() => satirSil(s.id)}
                                        title="Kaldır">🗑</button
                                    >
                                {:else}
                                    <button
                                        onclick={() =>
                                            adetDegistir(s.id, s.quantity - 1)}
                                        >-</button
                                    >
                                {/if}
                                <span class="adet-sayi">{s.quantity}</span>
                                <button
                                    onclick={() =>
                                        adetDegistir(s.id, s.quantity + 1)}
                                    disabled={s.quantity >= s.stock}>+</button
                                >
                            </div>

                            <div class="satir-tutar">{fiyatKolon(s.satir_tutar)}</div>
                        </div>
                    {/each}
                </div>
            {/each}
        </div>
        
        <aside class="sepet-ozet">
            <span class="ozet-etiket">SEÇİLEN ÜRÜNLER ({sepet.adet})</span>
            <span class="ozet-tutar">{fiyatKolon(sepet.toplam)}</span>
            <!--                            Sipariş Sistemi HENÜZ Yok                           -->
        
            <button class="ozet-btn" disabled>Alışverişi Tamamla</button>
            <p class="ozet-not">Sipariş oluşturma geliştirme aşamasında!</p>
        </aside>
    </div>
{:else}
    <div class="sepet-bos">
        <p>Sepetiniz boş.</p>
    </div>
{/if}

<OnayModal
  bind:acik={onayAcik}
  baslik="Sepeti Temizle"
  mesaj="Sepetinizdeki tüm ürünler kaldırılacak. Devam etmek istiyor musunuz?"
  onayYazi="Boşalt"
  onaylandi={sepetiBosalt}
/>
