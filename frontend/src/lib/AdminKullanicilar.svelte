<script>
    import { onMount } from "svelte";
    import {
        API,
        authHeader,
        durum,
        sayfalar,
        sayfala,
        toplamSayfa,
        sayfaGit,
    } from "./store.svelte.js";
    import KullaniciModal from "./KullaniciModal.svelte";

    let users = $state([]);
    let modalAcik = $state(false);

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
<button class="ekle-btn" onclick={() => (modalAcik = true)}
    >+ Yeni Kullanıcı</button
>

{#if users.length}
    <div class="tablo-cerceve">
        <table>
            <thead>
                <tr><th>ID</th><th>Kullanıcı</th><th>Rol</th></tr>
            </thead>
            <tbody>
                {#each sayfala(users, "users") as u}
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
        <span>Sayfa {sayfalar.users ?? 1} / {toplamSayfa(users)}</span>
        <button
            onclick={() => sayfaGit("users", 1)}
            disabled={(sayfalar.users ?? 1) === toplamSayfa(users)}
            >Sonraki</button
        >
    </div>
{:else}
    <p>Henüz kullanıcı yok.</p>
{/if}

<KullaniciModal bind:acik={modalAcik} eklendi={loadUsers} />
