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

<h2>Denetim Kayıtları</h2>
<div class="sekme-baslik">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya işlem ara...">
        {#snippet ekstra()}
            <select bind:value={secilenVarlik}>
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
    <div class="tablo-cerceve">
        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Kullanıcı</th>
                    <th>Rol</th>
                    <th>İşlem</th>
                    <th>Varlık</th>
                    <th>Kayıt</th>
                    <th>Eski Değer</th>
                    <th>Yeni Değer</th>
                    <!--<th>Ip</th>-->
                    <th>Tarih</th>
                </tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "denetim") as k}
                    <tr>
                        <td>{k.id}</td>
                        <td>{k.actor_username || "-"}</td>
                        <td>{k.actor_role || "-"}</td>
                        <td>{islemYazi(k.action)}</td>
                        <td>{k.entity}</td>
                        <td>{k.entity_id ?? "-"}</td>
                        <td class="kucuk ozellik-hucre" title={degerYazi(k.old_value)}>
                            {degerYazi(k.old_value)}
                        </td>
                        <td class="kucuk ozellik-hucre" title={degerYazi(k.new_value)}>
                            {degerYazi(k.new_value)}
                        </td>
                        <!-- <td>{k.ip_address || "-"}</td> -->
                        <td>{new Date(k.created_at).toLocaleString("tr-TR")}</td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
    <div class="pagination">
        <button onclick={() => sayfaGit("denetim", -1)}
                disabled={(sayfalar.denetim ?? 1) === 1}>Önceki</button>
        <span>Sayfa {sayfalar.denetim ?? 1} / {toplamSayfa(filtreli)}</span>
        <button onclick={() => sayfaGit("denetim", 1)}
                disabled={(sayfalar.denetim ?? 1) === toplamSayfa(filtreli)}>Sonraki</button>
    </div>
{:else}
    <p>Henüz denetim kaydı yok.</p>
{/if}
