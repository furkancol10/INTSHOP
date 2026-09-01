<script>
    import { onMount } from "svelte";
    import { API, authHeader, durum, fiyatKolon } from "../store.svelte";

    let talepler = $state([]);

    async function loadTalepler() {
        try {
            const res = await fetch(`${API}/api/requests/mine`, {
                headers: authHeader(),
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            talepler = await res.json();
        } catch (e) {
            durum.error = e instanceof Error ? e.message : String(e);
        }
    }

    async function iptalEt(id) {
        try {
            const res = await fetch(`${API}/api/requests/${id}/cancel`, {
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

    const durumlar = {
        pending: "Onay bekliyor",
        approved: "Onaylandı",
        rejected: "Reddedildi",
        cancelled: "İptal edildi",
    };

    function durumYazi(s) {
        return durumlar[s] || s;
    }

    onMount(loadTalepler);
</script>

<h2 class="text-2xl font-semibold mb-4">Taleplerim</h2>
{#if talepler.length}
        <table class="w-full border-collapse">
            <thead class="bg-coral-500 font-semibold">
                <tr>
                    <th class="p-2 border border-slate-100">Ürün</th>
                    <th class="p-2 border border-slate-100">Eski Fiyat</th>
                    <th class="p-2 border border-slate-100">Yeni Fiyat</th>
                    <th class="p-2 border border-slate-100">Durum</th>
                    <th class="p-2 border border-slate-100">Admin Notu</th>
                    <th class="p-2 border border-slate-100">Tarih</th>
                    <th class="p-2 border border-slate-100">İşlem</th>
                </tr>
            </thead>
            <tbody>
                {#each talepler as t}
                    <tr class="even:bg-yellow-100">
                        <td class="p-2 border border-slate-100 text-center">{t.urun}</td>
                        <td class="p-2 border border-slate-100 text-center">{fiyatKolon(t.old_price)}</td>
                        <td class="p-2 border border-slate-100 text-center">{fiyatKolon(t.new_price)}</td>
                        <td class="p-2 border border-slate-100 text-center"
                            ><span
                                class="px-[.6rem] py-[.25rem] rounded-full text-[.8rem] whitespace-nowrap {t.status === 'pending' ? 'bg-amber-100 text-amber-800' : t.status === 'approved' ? 'bg-green-100 text-green-800' : t.status === 'rejected' ? 'bg-red-100 text-red-800' : 'bg-gray-200 text-gray-700'}"
                                >{durumYazi(t.status)}</span
                            ></td
                        >
                        <td class="p-2 border border-slate-100 text-center">{t.admin_note || "-"}</td>
                        <td class="p-2 border border-slate-100 text-center">{new Date(t.created_at).toLocaleString("tr-TR")}</td
                        >
                        <td class="p-2 border border-slate-100 text-center">
                            {#if t.status === "pending"}
                                <button
                                    class="bg-orange-600 rounded-md w-[30px] text-right"
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
