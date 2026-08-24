<script>
  import { API, oturum } from "./store.svelte.js";

  let { girisYapildi } = $props();

  let kayitModu = $state(false);
  let loginUser = $state("");
  let loginPass = $state("");
  let loginError = $state("");
  let kayitMesaj = $state("");

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
      localStorage.setItem("token", oturum.token);
      localStorage.setItem("username", oturum.currentUser);
      localStorage.setItem("avatar_url", oturum.avatarUrl);
      girisYapildi();
    } catch (e) {
      loginError = e instanceof Error ? e.message : String(e);
    }
  }

  async function kayitOl() {
    kayitMesaj = "";
    loginError = "";
    if (epostaGecerli === false) {
      loginError = "Geçerli bir e-posta adresi girin";
      return;
    }
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
    }
  }
</script>

<div class="login-wrapper">
  <div class="lava-background">
    <div class="lava lava-1"></div>
    <div class="lava lava-2"></div>
    <div class="lava lava-3"></div>
    <div class="lava lava-4"></div>
    <div class="lava lava-5"></div>
    <div class="lava lava-6"></div>
    <div class="lava lava-7"></div>
  </div>

  <div class="flip-cerceve">
    <div class="flip-ic" class:donuk={kayitModu}>
      <div class="flip-yuz flip-on" inert={kayitModu}>
        <div class="login-card">
          <div class="avatar">
            <svg viewBox="0 0 24 24" width="40" height="40" fill="white">
              <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" />
            </svg>
          </div>

          <div class="input-group">
            <span class="input-icon">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="#888"><path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>
            </span>
            <input placeholder="Kullanıcı Adı" bind:value={loginUser} />
          </div>

          <div class="input-group">
            <span class="input-icon">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="#888"><path d="M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zm-6 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3-9H9V6c0-1.66 1.34-3 3-3s3 1.34 3 3v2z"/></svg>
            </span>
            <input type="password" placeholder="Şifre" bind:value={loginPass}
                   onkeydown={(e) => e.key === "Enter" && login()} />
          </div>

          <button class="login-btn" onclick={login}>Giriş</button>
          <button class="mod-degistir" onclick={() => { kayitModu = true; loginError = ""; kayitMesaj = ""; }}>
            Hesabın yok mu? Kayıt Ol
          </button>
          {#if kayitMesaj}<p class="basari">{kayitMesaj}</p>{/if}
          {#if loginError && !kayitModu}<p class="error">{loginError}</p>{/if}
        </div>
      </div>

      <div class="flip-yuz flip-arka" inert={!kayitModu}>
        <div class="login-card">
          <h3 class="kayit-baslik">Kayıt Ol</h3>
          <div class="input-group">
            <input placeholder="Kullanıcı Adı" bind:value={kayitForm.username} />
          </div>
          <div class="input-group">
            <input type="password" placeholder="Şifre (en az 6 karakter)" bind:value={kayitForm.password} />
          </div>
          <div class="input-group" class:hatali={epostaGecerli === false}>
            <input type="email" placeholder="E-Posta" bind:value={kayitForm.email} />
          </div>
          {#if epostaGecerli === false}
            <p class="ipucu-hata">Geçerli bir E-Posta giriniz. (ornek@site.com)</p>
          {/if}
          <div class="input-group">
            <input placeholder="Adres" bind:value={kayitForm.address} />
          </div>
          <div class="input-group">
            <input placeholder="Telefon" bind:value={kayitForm.phone} />
          </div>
          <button class="login-btn" onclick={kayitOl}>Kayıt Ol</button>
          <button class="mod-degistir" onclick={() => { kayitModu = false; loginError = ""; }}>
            Zaten hesabım var - Giriş Yap
          </button>
          {#if loginError && kayitModu}<p class="error">{loginError}</p>{/if}
        </div>
      </div>
    </div>
  </div>
</div>