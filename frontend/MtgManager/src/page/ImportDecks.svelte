<style>
    main {
        font-family: sans-serif;
        padding: 1rem;
    }
    form {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1rem;
    }
    .status {
        font-size: 0.9rem;
        color: #555;
    }
</style>

<main>
    <h1>Import Decks</h1>

    <form on:submit|preventDefault={handleSubmit}>
        <!-- TODO WESD disable this and submit button until fetching initial user is done. -->
        <input 
            type="text" 
            bind:value={archidektUser} 
            placeholder="Loading..." 
            required
        />
        <!-- TODO WESD change this to 'scrape archidekt', have backend save user, scrape data, then return decks -->
        <button type="submit">Submit</button>
    </form>

    {#if statusMessage}
        <p class="status">{statusMessage}</p>
    {/if}
</main>

<script>
    import { onMount } from 'svelte';

    let archidektUser = '';
    let statusMessage = '';

    // Fetch the user on load
    onMount(async () => {
        try {
            const response = await fetch('http://localhost:5070/ImportDecks/ArchidektUser');
            if (response.ok) {
                archidektUser = await response.json(); 
            } else {
                archidektUser = ""; 
                statusMessage = 'Error fetching user.';
            }
        } catch (error) {
            console.error('Error fetching user:', error);
            statusMessage = 'Error fetching user.';
        }
    });

    async function handleSubmit() {
        statusMessage = 'Submitting...';
        
        try {
            const response = await fetch('http://localhost:5070/ImportDecks/ArchidektUser', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(archidektUser) 
            });

            if (response.ok) {
                statusMessage = 'Submission successful';
            } else {
                statusMessage = 'Submission failed.';
            }
        } catch (error) {
            console.error('Submission failed:', error);
            statusMessage = 'Submission failed.';
        }
    }
</script>