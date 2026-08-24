<script>
    import { onMount } from "svelte";
    import { API, authHeader, durum, fiyatKolon } from "./store.svelte";

    let talepler = $state([]);
    let filtre = $state("all");

    async function loadTalepler() {
        try {
            const res = await fetch(`${API}/api/requests/mine=${filtre}`, {
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            requests = await res.json();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    async function iptalEt(id) {
        try {
            const res = await fetch(`${API}/api/requests/mine/${id}/cancel`, {
                method: "PUT",
                headers: authHeader(),
            });
            if (!res.ok)
                throw new Error((await res.text()) || `HTTP ${res.status}`);
            await loadTalepler();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    onMount(loadTalepler);
</script>

<h2>Taleplerim</h2>
{#if talepler.length}
    <table>
        <thead>
            <tr>
                <th>Ürün</th>
                <th>Eski Fiyat</th>
                <th>Yeni Fiyat</th>
                <th>Durum</th>
                <th>Admin Notu</th>
                <th>Tarih</th>
                <th>İşlem</th>
            </tr>
        </thead>
        <tbody>
            {#each talepler as t}
                <tr>
                    <td>{t.urun}</td>
                    <td>{fiyatKolon(t.old_price)}</td>
                    <td>{fiyatKolon(t.new_price)}</td>
                    <td
                        ><span class="rozet {t.status}"
                            >{durumYaz(t.status)}</span
                        ></td
                    >
                    <td>{t.admin_note || "-"}</td>
                    <td>{new Date(t.created_at).toLocaleDateString("tr-TR")}</td
                    >
                    <td>
                        {#if t.status === "pending"}
                            <button
                                class="sil-btn"
                                onclick={() => iptalEt(t.id)}>İptal</button
                            >
                        {/if}
                    </td>
                </tr>
            {/each}
        </tbody>
    </table>
{:else}
    <p>Henüz talebiniz yok.</p>
{/if}
