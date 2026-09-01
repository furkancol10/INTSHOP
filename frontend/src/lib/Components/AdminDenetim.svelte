<script>
    import { onMount } from "svelte";
    import {
        API,
        authHeader,
        durum,
        metinAra,
        alanEsit,
        sayfalar,
        sayfala,
        toplamSayfa,
        sayfaGit,
    } from "../store.svelte.js";
    import FiltreCubuk from "./FiltreCubuk.svelte";

    let kayitlar = $state([]);

    let secilenVarlik = $state("");
    let arama = $state("");

    let filtreli = $derived(
        alanEsit(
            metinAra(kayitlar, arama, ["actor_username", "action"]),
            secilenVarlik,
            "entity",
        ),
    );

    $effect(() => {
        secilenVarlik;
        arama;
        sayfalar.denetim = 1;
    });

    async function loadKayitlar() {
        try {
            const res = await fetch(`${API}/api/audit?limit=200`, {
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            kayitlar = await res.json();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    const islemler = {
        "product.create": "Ürün Oluşturuldu",
        "product.update": "Ürün Güncellendi",
        "product.delete": "Ürün Silindi",
        "category.create": "Kategori Oluşturuldu",
        "category.delete": "Kategori Silindi",
        "request.approve": "Talep Onaylandı",
        "request.reject": "Talep Reddedildi",
        "user.create": "Kullanıcı Oluşturuldu",
        "stock.movement": "Stok Hareketi",
    };
    function islemYazi(a) {
        return islemler[a] || a;
    }

    // Backend jsonb kolonlarını (old_value/new_value) düz metin içinde JSON
    // olarak döndürüyor (nesne olarak değil) - önce onu çözümlememiz gerekiyor.
    function jsonCoz(v) {
        if (v === null || v === undefined) return null;
        if (typeof v !== "string") return v;
        try {
            return JSON.parse(v);
        } catch {
            return v;
        }
    }

    function degerYazi(v) {
        const ayrisik = jsonCoz(v);
        if (ayrisik === null || ayrisik === undefined) return "-";
        if (typeof ayrisik !== "object") return String(ayrisik);

        const parcalar = Object.entries(ayrisik).map(([anahtar, deger]) => {
            const altAyrisik = jsonCoz(deger);
            const yazi =
                altAyrisik !== null && typeof altAyrisik === "object"
                    ? JSON.stringify(altAyrisik)
                    : deger;
            return `${anahtar}: ${yazi}`;
        });
        return parcalar.join(", ") || "-";
    }

    onMount(loadKayitlar);
</script>

<h2 class="text-2xl font-semibold mb-4">Denetim Kayıtları</h2>
<div class="flex items-center justify-between mb-4">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya işlem ara...">
        {#snippet ekstra()}
            <select bind:value={secilenVarlik} class="p-2 border border-slate-200 rounded-md">
                <option value="">Tüm varlıklar</option>
                <option value="products">Ürünler</option>
                <option value="categories">Kategoriler</option>
                <option value="requests">Talepler</option>
                <option value="users">Kullanıcılar</option>
                <option value="dealer_stock">Bayi Stoğu</option>
            </select>
        {/snippet}
    </FiltreCubuk>
</div>

{#if filtreli.length}
    <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
        <table class="w-full border-collapse">
            <thead class="bg-coral-500 font-semibold">
                <tr>
                    <th class="p-2 border border-slate-100">#</th>
                    <th class="p-2 border border-slate-100">Kullanıcı</th>
                    <th class="p-2 border border-slate-100">Rol</th>
                    <th class="p-2 border border-slate-100">İşlem</th>
                    <th class="p-2 border border-slate-100">Varlık</th>
                    <th class="p-2 border border-slate-100">Kayıt</th>
                    <th class="p-2 border border-slate-100">Eski Değer</th>
                    <th class="p-2 border border-slate-100">Yeni Değer</th>
                    <th class="p-2 border border-slate-100">Tarih</th>
                </tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "denetim") as k}
                    <tr class="even:bg-yellow-100">
                        <td class="p-2 border border-slate-100 text-center">{k.id}</td>
                        <td class="p-2 border border-slate-100 text-center">{k.actor_username || "-"}</td>
                        <td class="p-2 border border-slate-100 text-center">{k.actor_role || "-"}</td>
                        <td class="p-2 border border-slate-100 text-center">{islemYazi(k.action)}</td>
                        <td class="p-2 border border-slate-100 text-center">{k.entity}</td>
                        <td class="p-2 border border-slate-100 text-center">{k.entity_id ?? "-"}</td>
                        <td class="text-xs text-gray-500 max-w-[220px] truncate p-2 border border-slate-100" title={degerYazi(k.old_value)}>
                            {degerYazi(k.old_value)}
                        </td>
                        <td class="text-xs text-gray-500 max-w-[220px] truncate p-2 border border-slate-100" title={degerYazi(k.new_value)}>
                            {degerYazi(k.new_value)}
                        </td>
                        <td class="p-2 border border-slate-100 text-center">{new Date(k.created_at).toLocaleString("tr-TR")}</td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
    <div class="flex items-center gap-4 mt-4">
        <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" 
                onclick={() => sayfaGit("denetim", -1)}
                disabled={(sayfalar.denetim ?? 1) === 1}>Önceki</button>
        <span>Sayfa {sayfalar.denetim ?? 1} / {toplamSayfa(filtreli)}</span>
        <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" 
                onclick={() => sayfaGit("denetim", 1)}
                disabled={(sayfalar.denetim ?? 1) === toplamSayfa(filtreli)}>Sonraki</button>
    </div>
{:else}
    <p>Henüz denetim kaydı yok.</p>
{/if}
