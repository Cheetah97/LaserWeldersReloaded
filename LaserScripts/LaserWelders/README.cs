/* README
 * 
 * The Laser Welders are built on a modular framework called EEMKernel which provides
 * handy implementations for many routine operations, such as syncing, config, and semi-auto terminal control creation.
 * It is built around the idea of Single Responsibility Principle - that is, each part of the functionality
 * (working on blocks, drawing beams, adjusting power consumption, saving term controls, fiddling with inventories, etc)
 * is designed to work as a semi-standalone module and to be governed by an extension to MyGameLogicComponent
 * known as the EntityKernel. Another extension to this, the TerminalBlockKernel, includes default implementations
 * for manipulating and saving terminal controls with automated synchronization.
 * 
 * LaserWelders\EntityModules folder contains the modules that are used by various mod entities, including
 * the Laser Multitool itself, an extended Projector logic module, Pyrobolts, and a HUD drawer for cockpits.
 * The names of the modules should be pretty self-descriptive.
 * 
 * As such, there is pretty much only one single rule: do not slap new, unrelated functionality in existing modules,
 * design a new one and load it with the kernel.
 */