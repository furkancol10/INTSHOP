<script>
  import { API, oturum } from "../store.svelte.js";

  let { girisYapildi } = $props();

  let kayitModu = $state(false);
  let loginUser = $state("");
  let loginPass = $state("");
  let loginError = $state("");
  let kayitMesaj = $state("");
  let girisYukleniyor = $state(false);
  let kayitYukleniyor = $state(false);

  let kayitForm = $state({
    username: "",
    password: "",
    email: "",
    address: "",
    phone: "",
  });

  let epostaGecerli = $derived.by(() => {
    const e = kayitForm.email?.trim() || "";
    if (!e) return null;
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e);
  });

  async function login() {
    loginError = "";
    girisYukleniyor = true;
    try {
      const res = await fetch(`${API}/api/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: loginUser, password: loginPass }),
      });
      if (!res.ok) throw new Error("Kullanıcı adı veya şifre hatalı");
      const data = await res.json();
      oturum.token = data.token;
      oturum.role = data.role;
      oturum.currentUser = data.username;
      oturum.avatarUrl = data.avatar_url || "";
      oturum.sifreDegistir = data.sifre_degistir || false;
      localStorage.setItem("token", oturum.token);
      localStorage.setItem("username", oturum.currentUser);
      localStorage.setItem("avatar_url", oturum.avatarUrl);
      girisYapildi();
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    } finally {
      girisYukleniyor = false;
    }
  }

  async function kayitOl() {
    kayitMesaj = "";
    loginError = "";
    if (epostaGecerli === false) {
      loginError = "Geçerli bir e-posta adresi girin";
      return;
    }
    kayitYukleniyor = true;
    try {
      const res = await fetch(`${API}/api/signup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(kayitForm),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      loginUser = kayitForm.username;
      loginPass = kayitForm.password;
      kayitForm = { username: "", password: "", email: "", address: "", phone: "" };
      kayitModu = false;
      await login();
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    } finally {
      kayitYukleniyor = false;
    }
  }
</script>

<div class="text-center items-center flex min-h-screen justify-center relative overflow-hidden bg-[linear-gradient(135deg,rgb(153,0,255),blue,lightseagreen,lightblue)]">
  <div class="lava-background">
    <div class="lava lava-1"></div>
    <div class="lava lava-2"></div>
    <div class="lava lava-3"></div>
    <div class="lava lava-4"></div>
    <div class="lava lava-5"></div>
    <div class="lava lava-6"></div>
    <div class="lava"></div>
  </div>

  <div class="[perspective:1200px] relative z-[1] w-[340px] min-h-[520px] animate-fade-in-up">
    <div
      class="relative w-full transition-transform duration-[800ms] [transition-timing-function:cubic-bezier(0.34,1.56,0.64,1)] [transform-style:preserve-3d] {kayitModu ? '[transform:rotateY(180deg)]' : ''}"
    >
      <div class="[backface-visibility:hidden]" inert={kayitModu}>
        <div class="relative z-10 bg-white/70 backdrop-blur-xl border border-white/60 py-10 px-8 rounded-[20px] shadow-[0_20px_60px_-15px_rgba(0,0,0,0.45)] w-full box-border flex flex-col gap-[1.2rem] items-center transition-shadow duration-300 hover:shadow-[0_24px_70px_-12px_rgba(0,0,0,0.5)]">
          <div class="relative mb-2">
            <div class="absolute inset-0 rounded-full bg-teal-500/40 blur-md animate-pulse"></div>
            <div class="relative w-20 h-20 rounded-full bg-gradient-to-br from-teal-500 to-purple-600 ring-4 ring-white/70 shadow-lg shadow-teal-600/30 flex items-center justify-center">
              <svg viewBox="0 0 24 24" width="40" height="40" fill="white">
                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
              </svg>
            </div>
          </div>

          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <span class="px-[0.6rem] flex items-center text-gray-400 transition-colors">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>
            </span>
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" placeholder="Kullanıcı Adı" bind:value={loginUser} />
          </div>

          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <span class="px-[0.6rem] flex items-center text-gray-400 transition-colors">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3-9H9V6c0-1.66 1.34-3 3-3s3 1.34 3 3v2z"/></svg>
            </span>
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" type="password" placeholder="Şifre" bind:value={loginPass}
                   onkeydown={(e) => e.key === "Enter" && login()} />
          </div>

          <button
            class="w-full p-[0.8rem] bg-gradient-to-r from-teal-600 to-teal-500 text-white border-none rounded-lg text-base tracking-[1px] cursor-pointer transition-all duration-200 shadow-md shadow-teal-600/30 hover:from-teal-500 hover:to-teal-400 hover:shadow-lg hover:shadow-teal-600/40 hover:-translate-y-0.5 active:translate-y-0 active:scale-[0.97] disabled:opacity-60 disabled:cursor-not-allowed disabled:translate-y-0 flex items-center justify-center gap-2"
            onclick={login}
            disabled={girisYukleniyor}
          >
            {#if girisYukleniyor}
              <span class="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
            {/if}
            Giriş
          </button>
          <button class="bg-transparent border-none text-gray-600 text-[0.85rem] cursor-pointer underline decoration-transparent hover:decoration-teal-600 underline-offset-4 p-[0.3rem] transition-all duration-200 hover:text-teal-700" onclick={() => { kayitModu = true; loginError = ""; kayitMesaj = ""; }}>
            Hesabın yok mu? Kayıt Ol
          </button>
          {#if kayitMesaj}<p class="text-green-600 text-[0.85rem] m-0 text-center">{kayitMesaj}</p>{/if}
          {#if loginError && !kayitModu}<p class="text-red-700 text-[0.85rem] m-0 animate-shake">{loginError}</p>{/if}
        </div>
      </div>

      <div class="[backface-visibility:hidden] absolute top-0 left-0 w-full [transform:rotateY(180deg)]" inert={!kayitModu}>
        <div class="relative z-10 bg-white/70 backdrop-blur-xl border border-white/60 py-10 px-8 rounded-[20px] shadow-[0_20px_60px_-15px_rgba(0,0,0,0.45)] w-full box-border flex flex-col gap-[1.2rem] items-center transition-shadow duration-300 hover:shadow-[0_24px_70px_-12px_rgba(0,0,0,0.5)]">
          <h3 class="[perspective:1200px] w-full">Kayıt Ol</h3>
          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" placeholder="Kullanıcı Adı" bind:value={kayitForm.username} />
          </div>
          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" type="password" placeholder="Şifre (en az 8 karakter, harf ve rakam)" bind:value={kayitForm.password} />
          </div>
          <div
            class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]"
            class:ring-red-500={epostaGecerli === false}
            class:ring-2={epostaGecerli === false}
          >
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" type="email" placeholder="E-Posta" bind:value={kayitForm.email} />
          </div>
          {#if epostaGecerli === false}
            <p class="-mt-[0.6rem] text-[0.8rem] text-red-600 self-start animate-shake">Geçerli bir E-Posta giriniz. (ornek@site.com)</p>
          {/if}
          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" placeholder="Adres" bind:value={kayitForm.address} />
          </div>
          <div class="flex items-center w-full bg-white/70 rounded-lg overflow-hidden ring-1 ring-slate-200 transition-all duration-200 focus-within:ring-2 focus-within:ring-teal-500 focus-within:bg-white focus-within:shadow-[0_0_0_4px_rgba(13,148,136,0.12)]">
            <input class="flex-1 py-[0.7rem] px-2 border-none bg-transparent text-base outline-none" placeholder="Telefon" bind:value={kayitForm.phone} />
          </div>
          <button
            class="w-full p-[0.8rem] bg-gradient-to-r from-teal-600 to-teal-500 text-white border-none rounded-lg text-base tracking-[1px] cursor-pointer transition-all duration-200 shadow-md shadow-teal-600/30 hover:from-teal-500 hover:to-teal-400 hover:shadow-lg hover:shadow-teal-600/40 hover:-translate-y-0.5 active:translate-y-0 active:scale-[0.97] disabled:opacity-60 disabled:cursor-not-allowed disabled:translate-y-0 flex items-center justify-center gap-2"
            onclick={kayitOl}
            disabled={kayitYukleniyor}
          >
            {#if kayitYukleniyor}
              <span class="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin"></span>
            {/if}
            Kayıt Ol
          </button>
          <button class="bg-transparent border-none text-gray-600 text-[0.85rem] cursor-pointer underline decoration-transparent hover:decoration-teal-600 underline-offset-4 p-[0.3rem] transition-all duration-200 hover:text-teal-700" onclick={() => { kayitModu = false; loginError = ""; }}>
            Zaten hesabım var - Giriş Yap
          </button>
          {#if loginError && kayitModu}<p class="text-red-700 text-[0.85rem] m-0 animate-shake">{loginError}</p>{/if}
        </div>
      </div>
    </div>
  </div>
</div>