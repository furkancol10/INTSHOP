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
    import KullaniciModal from "../Modals/KullaniciModal.svelte";
    import FiltreCubuk from "../Components/FiltreCubuk.svelte";

    let users = $state([]);
    let modalAcik = $state(false);

    let arama = $state("");

    let filtreli = $derived(metinAra(users, arama, ["username", "role"]));

    $effect(() => {
        arama;
        sayfalar.users = 1;
    });

    async function loadUsers() {
        try {
            const res = await fetch(`${API}/api/users`, {
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            users = await res.json();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    onMount(loadUsers);
</script>

<h2 class="text-2xl font-semibold mb-4">Kullanıcılar</h2>
<div class="flex items-center justify-between mb-[1.2rem]">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya rol ara..."></FiltreCubuk>
    <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={() => (modalAcik = true)}
        >+ Yeni Kullanıcı</button
    >
</div>

{#if filtreli.length}
    <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
        <table class="w-full border-collapse">
            <thead class="bg-coral-500 font-semibold">
                <tr><th class="p-2 border border-slate-100">ID</th><th class="p-2 border border-slate-100">Kullanıcı</th><th class="p-2 border border-slate-100">Rol</th></tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "users") as u}
                    <tr class="even:bg-yellow-100">
                        <td class="p-2 border border-slate-100 text-center">{u.id}</td>
                        <td class="p-2 border border-slate-100 text-center">{u.username}</td>
                        <td class="p-2 border border-slate-100 text-center">{u.role}</td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
    <div class="flex items-center gap-4 mt-4">
        <button
            class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
            onclick={() => sayfaGit("users", -1)}
            disabled={(sayfalar.users ?? 1) === 1}>Önceki</button
        >
        <span>Sayfa {sayfalar.users ?? 1} / {toplamSayfa(filtreli)}</span>
        <button
            class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
            onclick={() => sayfaGit("users", 1)}
            disabled={(sayfalar.users ?? 1) === toplamSayfa(filtreli)}
            >Sonraki</button
        >
    </div>
{:else}
    <p>Henüz kullanıcı yok.</p>
{/if}

<KullaniciModal bind:acik={modalAcik} eklendi={loadUsers} />
