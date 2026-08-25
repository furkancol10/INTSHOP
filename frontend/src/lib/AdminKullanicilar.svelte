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
    } from "./store.svelte.js";
    import KullaniciModal from "./KullaniciModal.svelte";
    import FiltreCubuk from "./FiltreCubuk.svelte";

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

<h2>Kullanıcılar</h2>
<div class="sekme-baslik">
    <FiltreCubuk bind:arama placeholder="Kullanıcı veya rol ara..."></FiltreCubuk>
    <button class="ekle-btn" onclick={() => (modalAcik = true)}
        >+ Yeni Kullanıcı</button
    >
</div>

{#if filtreli.length}
    <div class="tablo-cerceve">
        <table>
            <thead>
                <tr><th>ID</th><th>Kullanıcı</th><th>Rol</th></tr>
            </thead>
            <tbody>
                {#each sayfala(filtreli, "users") as u}
                    <tr>
                        <td>{u.id}</td>
                        <td>{u.username}</td>
                        <td>{u.role}</td>
                    </tr>
                {/each}
            </tbody>
        </table>
    </div>
    <div class="pagination">
        <button
            onclick={() => sayfaGit("users", -1)}
            disabled={(sayfalar.users ?? 1) === 1}>Önceki</button
        >
        <span>Sayfa {sayfalar.users ?? 1} / {toplamSayfa(filtreli)}</span>
        <button
            onclick={() => sayfaGit("users", 1)}
            disabled={(sayfalar.users ?? 1) === toplamSayfa(filtreli)}
            >Sonraki</button
        >
    </div>
{:else}
    <p>Henüz kullanıcı yok.</p>
{/if}

<KullaniciModal bind:acik={modalAcik} eklendi={loadUsers} />
