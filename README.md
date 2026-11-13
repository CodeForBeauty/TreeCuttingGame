# TreeCuttingGame

A game about cutting trees

## Overview

This project demonstrates a proof of concept for sdf based destruction system.

## Key features

In editor sdf generation.

Raymarched sdf rendering.

Raymarched raycast hit detection.

Runtime sdf editing.

## Possible improvements

UV coordinates are interpolated based on texel's location. This makes the UV seams visible.

SDF generation is quite costly. It is done in editor but the developer experience of waiting 10 minutes for sdf to generate is not ideal. Moving generation to compute shader probably would be the best solution.

Runtime sdf editing is also quite expensive. Improving it's performance would boost visuals of the system.

SDFs right now take a lot of space. Using some alternative storing method would fix this.
