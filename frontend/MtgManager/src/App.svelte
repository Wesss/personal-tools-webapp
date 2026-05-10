<script lang="ts">
	import type { Component } from 'svelte';
  import ImportDecks from './page/ImportDecks.svelte'

  const menuDict: { [key: string]: Component } = {
    "ImportDecks" : ImportDecks,
  };

	let activePage : Component = menuDict["ImportDecks"];

  function setPage(event: Event, component: Component) {
    event.preventDefault();
    activePage = component;
  }
</script>

<div id="page">
  <nav>
    <div id="menu">
      <h3>Mtg Manager</h3>
      {#each Object.keys(menuDict) as key}
        <a href="/" onclick={(event) => setPage(event, menuDict[key])}>{key}</a>
      {/each}
    </div>
  </nav>
  <main>
    {#if !activePage}
      <h1>Page Not Found</h1>
    {:else}
      <svelte:component this={activePage} />
    {/if}
  </main>
</div>

<style>
  #page {
    min-width: 480px;
    min-height: 100vh;
  }

  nav {
    border-bottom: 1px solid black;
  }

  #menu {
    display: flex;
    gap: 10px;
    align-items: baseline;
    margin: 0 20px;
  }

  main {
    margin: 20px;
  }
</style>