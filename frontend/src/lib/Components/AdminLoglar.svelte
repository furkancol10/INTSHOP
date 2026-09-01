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

<h2 class="text-2xl font-semibold mb-4">Loglar</h2>
<div class="flex items-center justify-between mb-[1.2rem]">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya rol ara...">
        {#snippet ekstra()}
            <select bind:value={secilenIslem} class="p-2 border border-slate-200 rounded-md">
                <option value="">Tümü</option>
                <option value="login">Giriş</option>
                <option value="logout">Çıkış</option>
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
                    <th class="p-2 border border-slate-100">Ip</th>
                    <th class="p-2 border border-slate-100">Tarih</th>
                </tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "loglar") as l}
                    <tr class="even:bg-yellow-100">
                        <td class="p-2 border border-slate-100 text-center">{l.id}</td>
                        <td class="p-2 border border-slate-100 text-center">{l.username}</td>
                        <td class="p-2 border border-slate-100 text-center">{l.role}</td>
                        <td class="p-2 border border-slate-100 text-center">{islemYazi(l.action)}</td>
                        <td class="p-2 border border-slate-100 text-center">{l.ip_address || "-"}</td>
                        <td class="p-2 border border-slate-100 text-center">{new Date(l.created_at).toLocaleString("tr-TR")}</td
                        >
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
{:else}
    <p>Henüz log yok.</p>
{/if}
