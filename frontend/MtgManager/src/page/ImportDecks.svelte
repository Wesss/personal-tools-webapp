<script lang="ts">
    let deckText = "";
    let importPromise: Promise<any>;

    // TODO WESD make an actual ajax postback to import decks
    async function importDecks() {
        const response = await fetch("http://localhost:5070/ImportDecks", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(deckText)
            // body: JSON.stringify({ deckText }),
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        importPromise = await response.json();
    }
</script>

<h1>Import Decks</h1>
<div>
    <textarea name="deckText" rows="30" cols="100" bind:value={deckText}></textarea>
</div>
<div>
    <button onclick={importDecks}>Submit</button>
</div>

<!-- TODO WESD debug why this always is showing as success. we want to hide before button is clicked. -->
{#await importPromise}
    Loading...
{:then data}
    <div class="success">success!</div>
    {JSON.stringify(data, null, 2)}
{:catch error}
    <div class="error">{error}</div>
{/await}

<style>
    .success {
        color: green;
    }
    .error {
        color: red;
    }
</style>
