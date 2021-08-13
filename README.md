# Fig
**Ship configuration to production safely, predictably, and quickly.**

Fig is a configuration (&rarr; config &rarr; fig, get it?) system designed to solve the problem
of how one safely, predictably, and quickly ships configuration changes to production systems.
It is designed around the idea that your systems shouldn't depend on Fig to work correctly and
Fig shouldn't, through any fault of its own, prevent your systems from operating correctly.
That's a tall order and probably one we'll fail at, large scale production systems being what
they are, but it informs every decision about how Fig operates.

Fig is composed of three critical components: a server, an agent, and a client library. These
components collaborate with one another to deploy new configuration versions to your infrastructure
and are designed to operate correctly even if the component left of them isn't available.

## Example
```bash
echo "crash_on_start: true" > config.yml

# Build a configuration manifest containing your config file
fig manifest build 0.1-broken --filter config.yml

# Verify that your manifest is valid
fig manifest verify

# Initialize the local configuration store
fig init

# Import your configuration into the local config store
fig version import

# Switch to your new config
fig version set 0.1-broken

# Check the content of your new config file
fig file cat config.yml

# Oops, that's crashing the process, let's fix it...
echo "crash_on_start: false" > config.yml
fig manifest build 0.1-fixed --filter config.yml
fig version import
fig version set 0.1-fixed

# Confirm that the config is now correct
fig file cat config.yml
```