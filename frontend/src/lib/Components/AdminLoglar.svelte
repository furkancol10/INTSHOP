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

    let loglar = $state([]);

    //filtreleme
    let secilenIslem = $state("");
    let arama = $state("");

    let filtreli = $derived(
        alanEsit(
            metinAra(loglar, arama, ["username", "role"]),
            secilenIslem,
            "action",
        ),
    );

    $effect(() => {
        secilenIslem;
        arama;
        sayfalar.loglar = 1;
    });

    async function loadLoglar() {
        try {
            const res = await fetch(`${API}/api/logs`, {
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            loglar = await res.json();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    const islemler = { login: "Giriş", logout: "Çıkış" };
    function islemYazi(a) {
        return islemler[a] || a;
    }

    onMount(loadLoglar);
</script>

<h2>Loglar</h2>
<div class="sekme-baslik">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya rol ara...">
        {#snippet ekstra()}
            <select bind:value={secilenIslem}>
                <option value="">Tümü</option>
                <option value="login">Giriş</option>
                <option value="logout">Çıkış</option>
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
                    <th>Ip</th>
                    <th>Tarih</th>
                </tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "loglar") as l}
                    <tr>
                        <td>{l.id}</td>
                        <td>{l.username}</td>
                        <td>{l.role}</td>
                        <td>{islemYazi(l.action)}</td>
                        <td>{l.ip_address || "-"}</td>
                        <td>{new Date(l.created_at).toLocaleString("tr-TR")}</td
                        >
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
{:else}
    <p>Henüz log yok.</p>
{/if}
