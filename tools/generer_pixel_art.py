#!/usr/bin/env python3
"""Dessine tout le pixel art du tableau 1 (la forêt) de « La Porte des 5 gemmes ».

Chaque image est dessinée par du code, pixel par pixel, puis enregistrée en PNG dans
PorteDes5Gemmes/Assets/_Game/Resources/Art. Tu peux ensuite retoucher ces PNG à la main
(Aseprite, LibreSprite, Piskel...) : Unity les réimporte tout seul.

Repère : l'écran de référence fait 320 x 180 pixels. Le fond fait 400 x 260 pour couvrir
aussi les téléphones très allongés et les tablettes.

Usage : python3 tools/generer_pixel_art.py
"""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

from PIL import Image

RACINE = Path(__file__).resolve().parent.parent
SORTIE = RACINE / "PorteDes5Gemmes" / "Assets" / "_Game" / "Resources" / "Art"
SCENE = RACINE / "PorteDes5Gemmes" / "Assets" / "_Game" / "Resources" / "Tableaux" / "foret.json"

# ─── Palette ──────────────────────────────────────────────────────────────────


def hexa(c: str, a: int = 255):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a)


P = {k: hexa(v) for k, v in {
    "nuit": "#0b1f2a", "ciel1": "#0f2f35", "ciel2": "#123d3a", "ciel3": "#1d5244", "ciel4": "#2f6b4f",
    "lune": "#f6f0c8", "lune2": "#ddd3a0", "lune3": "#c4b985",
    "loin1": "#163f38", "loin2": "#1c4c3f", "loin3": "#245a45",
    "sol": "#3b7a3a", "sol_clair": "#4f9a44", "sol_fonce": "#2c5e2f", "sol_tres_fonce": "#224a27",
    "chemin": "#8a7350", "chemin_clair": "#a68b5f", "chemin_fonce": "#6c583c",
    "tronc": "#6b4430", "tronc_clair": "#8a5a3c", "tronc_fonce": "#3e2719",
    "feuille": "#2e7d3a", "feuille_clair": "#43a04b", "feuille_tres_clair": "#6cc067", "feuille_fonce": "#1f5c2c",
    "pin": "#1f6b45", "pin_clair": "#2d8a58", "pin_tres_clair": "#4aa872", "pin_fonce": "#14492f",
    "roche": "#7b878c", "roche_clair": "#a3aeb2", "roche_fonce": "#535d62",
    "buisson": "#2f8a3e", "buisson_clair": "#4fb05a", "buisson_fonce": "#1d5f29",
    "herbe": "#5bb04f", "herbe_fonce": "#357a35",
    "pied": "#fff3e0", "pied_ombre": "#d8c3ab", "blanc": "#ffffff",
    "bois": "#b07a48", "bois_clair": "#cf9a62", "bois_fonce": "#7a4f2c", "bois_tres_fonce": "#4e3019",
    "texte": "#3e2723", "creme": "#fff8e1", "creme_ombre": "#e8d9b0",
    "osier": "#c4914f", "osier_clair": "#e0b06a", "osier_fonce": "#8a5f2e", "osier_tres_fonce": "#5a3b1b",
    "pierre": "#5a5f73", "pierre_clair": "#7c8299", "pierre_fonce": "#363a4a",
    "hibou": "#8d6e63", "hibou_clair": "#bcaaa4", "hibou_fonce": "#5d4037", "oeil": "#ffd54f",
    "oiseau": "#4fc3f7", "oiseau_fonce": "#0288d1", "bec": "#ffb300",
    "fleur": "#f48fb1", "fleur_clair": "#fce4ec", "fleur_coeur": "#ffeb3b", "tige": "#388e3c",
    "lumi": "#fff59d", "lumi_coeur": "#ffffff", "aile": "#b3e5fc", "aile_fonce": "#81d4fa",
    "peau": "#ffcc9e", "peau_ombre": "#e8a878",
}.items()}

COULEURS_CHAMPIGNON = {
    "red": ("#e53935", "#ff7a6b", "#9e1f1c"),
    "blue": ("#1e88e5", "#6ab7f5", "#0d4f91"),
    "yellow": ("#fdd835", "#fff38a", "#b59a12"),
    "green": ("#43a047", "#81c784", "#1b5e20"),
    "orange": ("#fb8c00", "#ffb74d", "#a85a00"),
    "purple": ("#8e24aa", "#ce93d8", "#4a148c"),
    "pink": ("#ec407a", "#f48fb1", "#a0174a"),
}

COULEURS_GEMME = {
    "red": ("#e53935", "#ff8a80", "#8e1414", "#ffd0cc"),
    "orange": ("#fb8c00", "#ffcc80", "#a04f00", "#ffe8c8"),
    "yellow": ("#fdd835", "#fff59d", "#a68a00", "#fffbe0"),
    "green": ("#43a047", "#a5d6a7", "#1b5e20", "#e0f5e0"),
    "blue": ("#1e88e5", "#90caf9", "#0d3c80", "#dcefff"),
    "purple": ("#8e24aa", "#e1bee7", "#4a148c", "#f6e6fa"),
}

TRANSPARENT = (0, 0, 0, 0)

# ─── Outils de dessin ─────────────────────────────────────────────────────────


def nouvelle(l: int, h: int) -> Image.Image:
    return Image.new("RGBA", (l, h), TRANSPARENT)


def px(img, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < img.width and 0 <= y < img.height and c is not None:
        img.putpixel((x, y), c)


def rect(img, x0, y0, x1, y1, c):
    for y in range(int(y0), int(y1) + 1):
        for x in range(int(x0), int(x1) + 1):
            px(img, x, y, c)


def ellipse(img, cx, cy, rx, ry, c, couleur_fn=None):
    """Ellipse pleine. couleur_fn(dx, dy) peut renvoyer une couleur par pixel (ombrage)."""
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            dx = (x + 0.5 - cx) / max(rx, 0.5)
            dy = (y + 0.5 - cy) / max(ry, 0.5)
            if dx * dx + dy * dy <= 1.0:
                px(img, x, y, couleur_fn(dx, dy) if couleur_fn else c)


def ligne(img, x0, y0, x1, y1, c, epaisseur=1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / max(n, 1)
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t
        if epaisseur <= 1:
            px(img, x, y, c)
        else:
            ellipse(img, x, y, epaisseur / 2, epaisseur / 2, c)


def contour(img, c, diagonales=False):
    """Ajoute un contour d'un pixel autour de toutes les zones opaques."""
    src = img.copy()
    voisins = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    if diagonales:
        voisins += [(-1, -1), (1, -1), (-1, 1), (1, 1)]
    for y in range(img.height):
        for x in range(img.width):
            if src.getpixel((x, y))[3] > 0:
                continue
            for dx, dy in voisins:
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.width and 0 <= yy < img.height and src.getpixel((xx, yy))[3] > 200:
                    img.putpixel((x, y), c)
                    break
    return img


def ombre_sol(img, cx, cy, rx, ry, alpha=60):
    ellipse(img, cx, cy, rx, ry, (0, 0, 0, alpha))


def coller(dest, src, x, y):
    dest.alpha_composite(src, (int(x), int(y)))


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def tramage(x, y, t):
    """Vrai si le pixel (x, y) doit prendre la couleur suivante (dégradé tramé, t entre 0 et 1)."""
    return t * 16 > BAYER[y % 4][x % 4] + 0.5


def sauver(img, nom):
    SORTIE.mkdir(parents=True, exist_ok=True)
    img.save(SORTIE / f"{nom}.png")


# ─── Police pixel (5 x 7) pour les chiffres et les initiales ─────────────────

POLICE = {
    "0": ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
    "1": ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
    "2": ["01110", "10001", "00001", "00110", "01000", "10000", "11111"],
    "3": ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
    "4": ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
    "5": ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
    "6": ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
    "7": ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
    "8": ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
    "9": ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
}


def glyphe(img, car, x0, y0, echelle, c, c_contour=None):
    motif = POLICE[car]
    if c_contour:
        for gy, rang in enumerate(motif):
            for gx, b in enumerate(rang):
                if b == "1":
                    rect(img, x0 + gx * echelle - 1, y0 + gy * echelle - 1,
                         x0 + gx * echelle + echelle, y0 + gy * echelle + echelle, c_contour)
    for gy, rang in enumerate(motif):
        for gx, b in enumerate(rang):
            if b == "1":
                rect(img, x0 + gx * echelle, y0 + gy * echelle,
                     x0 + gx * echelle + echelle - 1, y0 + gy * echelle + echelle - 1, c)


# ─── Fond de la forêt (400 x 260) ─────────────────────────────────────────────

FOND_L, FOND_H = 400, 260
HORIZON = 128  # rangée du fond où commence l'herbe (le centre de l'écran est à 130)


def fond_foret():
    rnd = random.Random(7)
    img = nouvelle(FOND_L, FOND_H)
    bandes = [P["nuit"], P["ciel1"], P["ciel2"], P["ciel3"], P["ciel4"]]
    for y in range(HORIZON + 4):
        t = (y / (HORIZON + 4)) * (len(bandes) - 1)
        i = min(int(t), len(bandes) - 2)
        f = t - i
        for x in range(FOND_L):
            px(img, x, y, bandes[i + 1] if tramage(x, y, f) else bandes[i])

    # Étoiles
    for _ in range(46):
        x, y = rnd.randrange(FOND_L), rnd.randrange(0, 80)
        c = (255, 255, 230, rnd.choice([120, 170, 230]))
        px(img, x, y, c)
        if rnd.random() < 0.15:
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                px(img, x + dx, y + dy, (255, 255, 230, 90))

    # Lune et son halo
    lx, ly = 300, 58
    for r, a in [(24, 18), (20, 28), (17, 40)]:
        ellipse(img, lx, ly, r, r, None, lambda dx, dy, a=a: (246, 240, 200, a))
    img2 = nouvelle(FOND_L, FOND_H)
    ellipse(img2, lx, ly, 13, 13, P["lune"],
            lambda dx, dy: P["lune2"] if dx + dy * 0.6 > 0.55 else P["lune"])
    for cx, cy, r in [(-4, -3, 2.5), (4, 3, 2), (-2, 6, 1.5), (5, -6, 1.2)]:
        ellipse(img2, lx + cx, ly + cy, r, r, P["lune3"])
    img.alpha_composite(img2)

    # Lignes d'arbres lointains (deux plans)
    for plan, (couleur, base, hmin, hmax, pas) in enumerate([
        (P["loin1"], HORIZON + 2, 20, 38, 9),
        (P["loin2"], HORIZON + 4, 12, 26, 7),
    ]):
        x = -rnd.randrange(pas)
        while x < FOND_L + 10:
            h = rnd.randrange(hmin, hmax)
            demi = max(4, h // 3)
            for yy in range(h):
                larg = int(demi * (yy / h)) + 1
                for xx in range(-larg, larg + 1):
                    px(img, x + xx, base - h + yy, couleur)
            x += pas + rnd.randrange(-2, 4)
        rect(img, 0, base - 3, FOND_L - 1, base + 2, couleur)

    # Herbe : bandes tramées du plus foncé (loin) au plus clair (près)
    bandes_sol = [P["loin3"], P["sol_fonce"], P["sol"], P["sol"], P["sol_clair"]]
    for y in range(HORIZON, FOND_H):
        t = ((y - HORIZON) / (FOND_H - HORIZON)) * (len(bandes_sol) - 1)
        i = min(int(t), len(bandes_sol) - 2)
        f = t - i
        for x in range(FOND_L):
            px(img, x, y, bandes_sol[i + 1] if tramage(x, y, f) else bandes_sol[i])
    # Petite crête de collines à l'horizon
    for x in range(FOND_L):
        h = int(2 + 2 * math.sin(x / 23.0) + 1.5 * math.sin(x / 9.0 + 1))
        for yy in range(h):
            px(img, x, HORIZON - yy, P["loin3"])

    # Chemin de terre qui serpente vers le centre (là où la gemme apparaîtra)
    for y in range(HORIZON + 6, FOND_H):
        t = (y - HORIZON) / (FOND_H - HORIZON)
        cx = 200 + math.sin(t * 3.4 + 0.6) * 26 * t
        demi = 3 + t * 34
        for x in range(int(cx - demi), int(cx + demi) + 1):
            bord = abs(x - cx) > demi - 2
            c = P["chemin_fonce"] if bord else (P["chemin_clair"] if tramage(x, y, 0.25 + 0.3 * t) else P["chemin"])
            px(img, x, y, c)
    # Cailloux du chemin
    for _ in range(40):
        y = rnd.randrange(HORIZON + 20, FOND_H)
        t = (y - HORIZON) / (FOND_H - HORIZON)
        cx = 200 + math.sin(t * 3.4 + 0.6) * 26 * t
        x = int(cx + rnd.uniform(-1, 1) * (t * 30))
        px(img, x, y, P["chemin_fonce"])
        px(img, x + 1, y, P["chemin_clair"])

    # Brins d'herbe et petites fleurs
    for _ in range(420):
        x, y = rnd.randrange(FOND_L), rnd.randrange(HORIZON + 4, FOND_H)
        if img.getpixel((x, y))[:3] in [P["chemin"][:3], P["chemin_clair"][:3], P["chemin_fonce"][:3]]:
            continue
        c = P["herbe"] if rnd.random() < 0.6 else P["sol_tres_fonce"]
        px(img, x, y, c)
        if y > HORIZON + 30:
            px(img, x, y - 1, c)
    for _ in range(26):
        x, y = rnd.randrange(FOND_L), rnd.randrange(HORIZON + 20, FOND_H - 4)
        c = rnd.choice([P["fleur"], P["fleur_coeur"], P["creme"], P["aile"]])
        px(img, x, y, c)

    sauver(img, "fond_foret")


# ─── Décor ────────────────────────────────────────────────────────────────────


def arbre(echelle):
    l, h = int(round(58 * echelle)), int(round(76 * echelle))
    img = nouvelle(l + 2, h + 2)
    cx = l / 2 + 1
    # Tronc
    tl = max(4, int(8 * echelle))
    th = int(30 * echelle)
    for y in range(h - th, h):
        for x in range(int(cx - tl / 2), int(cx + tl / 2)):
            c = P["tronc_clair"] if x < cx - tl / 6 else P["tronc"]
            if x >= cx + tl / 4:
                c = P["tronc_fonce"]
            px(img, x, y, c)
    # Racines
    for s in (-1, 1):
        ligne(img, cx + s * tl / 2, h - 3, cx + s * (tl / 2 + 3 * echelle), h - 1, P["tronc_fonce"], 2)
    # Feuillage : plusieurs boules ombrées
    boules = [(0, -0.62, 0.42), (-0.28, -0.52, 0.30), (0.28, -0.50, 0.30), (-0.16, -0.80, 0.28), (0.18, -0.78, 0.27)]

    def teinte(dx, dy):
        lum = -dx * 0.5 - dy * 0.8
        if lum > 0.55:
            return P["feuille_tres_clair"]
        if lum > 0.1:
            return P["feuille_clair"]
        if lum > -0.45:
            return P["feuille"]
        return P["feuille_fonce"]

    for bx, by, r in boules:
        ellipse(img, cx + bx * l, h + by * h, r * l, r * l * 0.9, None, teinte)
    contour(img, P["feuille_fonce"] if False else hexa("#143d22"))
    return img


def sapin(echelle):
    l, h = int(round(40 * echelle)), int(round(70 * echelle))
    img = nouvelle(l + 2, h + 2)
    cx = l / 2 + 1
    rect(img, cx - 2, h - 8 * echelle, cx + 1, h - 1, P["tronc"])
    etages = 4
    for e in range(etages):
        haut = h * 0.08 + e * h * 0.19
        bas = haut + h * 0.36
        demi_max = (l / 2) * (0.5 + 0.5 * (e + 1) / etages)
        for y in range(int(haut), int(bas)):
            t = (y - haut) / (bas - haut)
            demi = demi_max * t
            for x in range(int(cx - demi), int(cx + demi) + 1):
                if x < cx - demi * 0.3:
                    c = P["pin_clair"] if t > 0.2 else P["pin_tres_clair"]
                elif x > cx + demi * 0.35:
                    c = P["pin_fonce"]
                else:
                    c = P["pin"]
                px(img, x, y, c)
    contour(img, hexa("#0d3320"))
    return img


def buisson(echelle):
    l, h = int(round(32 * echelle)), int(round(20 * echelle))
    img = nouvelle(l + 2, h + 3)

    def teinte(dx, dy):
        lum = -dx * 0.4 - dy * 0.9
        return P["buisson_clair"] if lum > 0.35 else (P["buisson"] if lum > -0.4 else P["buisson_fonce"])

    for bx, by, r in [(0.3, 0.62, 0.26), (0.55, 0.5, 0.3), (0.75, 0.66, 0.24)]:
        ellipse(img, 1 + bx * l, by * h + 1, r * l, r * l * 0.75, None, teinte)
    rect(img, 2, h, l - 1, h, P["buisson_fonce"])
    contour(img, hexa("#123d1a"))
    return img


def rocher(echelle):
    l, h = int(round(24 * echelle)), int(round(15 * echelle))
    img = nouvelle(l + 2, h + 2)

    def teinte(dx, dy):
        lum = -dx * 0.6 - dy * 0.8
        return P["roche_clair"] if lum > 0.4 else (P["roche"] if lum > -0.3 else P["roche_fonce"])

    ellipse(img, l / 2 + 1, h * 0.62 + 1, l / 2, h * 0.6, None, teinte)
    rect(img, 1, h, l, h + 1, TRANSPARENT)
    px(img, l * 0.35, h * 0.35, P["blanc"])
    contour(img, hexa("#30383c"))
    return img


def touffe():
    img = nouvelle(13, 9)
    for x0, hh in [(2, 5), (4, 7), (6, 8), (8, 6), (10, 4)]:
        ligne(img, x0, 8, x0 + (1 if x0 > 6 else -1), 8 - hh, P["herbe"])
        px(img, x0, 8, P["herbe_fonce"])
    return img


def branche():
    """Branche de l'arbre où Lumi s'endort (inclinée vers le haut, longueur 30 px)."""
    img = nouvelle(34, 12)
    ligne(img, 1, 9, 31, 3, P["tronc"], 3)
    ligne(img, 1, 8, 31, 2, P["tronc_clair"], 1)
    ligne(img, 18, 6, 22, 1, P["tronc"], 2)
    for fx, fy in [(22, 1), (30, 2), (12, 5)]:
        ellipse(img, fx, fy, 2.5, 1.6, P["feuille_clair"])
    contour(img, P["tronc_fonce"])
    return img


def hibou(cligne):
    img = nouvelle(14, 16)
    ellipse(img, 7, 9.5, 5.5, 6, P["hibou"])
    ellipse(img, 7, 11, 3.5, 3.5, P["hibou_clair"])
    for x in (3, 10):  # aigrettes
        px(img, x, 2, P["hibou_fonce"])
        px(img, x, 3, P["hibou"])
    for ox in (4.5, 9.5):
        ellipse(img, ox, 7, 2.2, 2.2, P["creme"])
        if cligne:
            rect(img, ox - 1.5, 7, ox + 1, 7, P["hibou_fonce"])
        else:
            px(img, ox, 7, P["oeil"])
            px(img, ox - 0.5, 7, P["texte"])
    px(img, 7, 9, P["bec"])
    px(img, 7, 10, P["bec"])
    contour(img, hexa("#3e2a22"))
    return img


def oiseau(etat):
    img = nouvelle(14, 11)
    ellipse(img, 7, 6, 4, 3, P["oiseau"])
    ellipse(img, 10, 4, 2.5, 2.5, P["oiseau"])
    px(img, 11, 3, P["texte"])
    px(img, 13, 4, P["bec"])
    px(img, 12, 4, P["bec"])
    rect(img, 1, 5, 3, 6, P["oiseau_fonce"])  # queue
    if etat == "pose":
        ellipse(img, 6, 6, 2.5, 1.5, P["oiseau_fonce"])
        px(img, 7, 9, P["bec"])
        px(img, 9, 9, P["bec"])
    elif etat == "vol1":
        ligne(img, 6, 5, 4, 0, P["oiseau_fonce"], 2)
    else:
        ligne(img, 6, 7, 4, 10, P["oiseau_fonce"], 2)
    contour(img, hexa("#01579b"))
    return img


def fleur(ouverte):
    img = nouvelle(13, 14)
    ligne(img, 6, 13, 6, 6, P["tige"])
    px(img, 7, 10, P["tige"])
    px(img, 8, 9, P["tige"])
    if ouverte:
        for a in range(6):
            ang = a * math.pi / 3
            ellipse(img, 6 + math.cos(ang) * 3, 4.5 + math.sin(ang) * 3, 2, 2, P["fleur"])
        ellipse(img, 6, 4.5, 1.5, 1.5, P["fleur_coeur"])
    else:
        ellipse(img, 6, 4.5, 2, 3, P["fleur"])
        px(img, 6, 3, P["fleur_clair"])
    contour(img, hexa("#880e4f"))
    return img


# ─── Objets du jeu ────────────────────────────────────────────────────────────


def champignon(couleur):
    base, clair, fonce = (hexa(c) for c in COULEURS_CHAMPIGNON[couleur])
    img = nouvelle(24, 24)
    ombre_sol(img, 12, 22, 9, 1.6, 70)
    # Pied
    for y in range(13, 22):
        for x in range(8, 16):
            c = P["pied"] if x < 13 else P["pied_ombre"]
            px(img, x, y, c)
    rect(img, 9, 21, 14, 21, P["pied_ombre"])

    # Chapeau en dôme
    def teinte(dx, dy):
        lum = -dx * 0.5 - dy * 0.9
        return clair if lum > 0.55 else (base if lum > -0.25 else fonce)

    for y in range(2, 15):
        for x in range(1, 23):
            dx = (x + 0.5 - 12) / 11
            dy = (y + 0.5 - 14) / 12
            if dx * dx + dy * dy <= 1 and y <= 13:
                px(img, x, y, teinte(dx, dy))
    rect(img, 2, 13, 21, 13, fonce)
    # Points blancs
    for cx, cy, r in [(7, 7, 1.6), (13, 4.5, 1.3), (17, 8.5, 1.8), (11, 10, 1)]:
        ellipse(img, cx, cy, r, r, P["blanc"])
    contour(img, hexa("#2a1a14"))
    return img


def panier(partie):
    """Panier en osier : 'dos' (l'intérieur, derrière les champignons) et 'face' (devant)."""
    l, h = 52, 30
    img = nouvelle(l, h)
    if partie == "dos":
        ellipse(img, l / 2, 9, 23, 5, P["osier_tres_fonce"])
        # Anse
        for a in range(0, 181, 2):
            ang = math.radians(a)
            px(img, l / 2 + math.cos(ang) * 19, 9 - math.sin(ang) * 8, P["osier_fonce"])
            px(img, l / 2 + math.cos(ang) * 19, 8 - math.sin(ang) * 8, P["osier"])
    else:
        for y in range(10, 28):
            t = (y - 10) / 18
            demi = 23 - t * 5
            for x in range(int(l / 2 - demi), int(l / 2 + demi) + 1):
                tresse = ((x // 3) + (y // 3)) % 2 == 0
                c = P["osier_clair"] if tresse else P["osier"]
                if x > l / 2 + demi * 0.5:
                    c = P["osier_fonce"] if tresse else P["osier"]
                px(img, x, y, c)
        ellipse(img, l / 2, 10, 24, 2.2, P["osier_clair"])
        rect(img, l / 2 - 17, 27, l / 2 + 17, 27, P["osier_tres_fonce"])
        contour(img, P["osier_tres_fonce"])
    return img


def panneau(nombre):
    """Panneau en bois avec un gros chiffre (le choix « combien ? »)."""
    l, h = 42, 44
    img = nouvelle(l, h)
    ombre_sol(img, l / 2, h - 2, 12, 1.5, 70)
    rect(img, l / 2 - 2, 30, l / 2 + 1, h - 3, P["bois_fonce"])  # poteau
    for y in range(2, 32):
        for x in range(2, l - 2):
            planche = (y - 2) // 10
            c = P["bois_clair"] if (y - 2) % 10 == 0 else P["bois"]
            if (y - 2) % 10 == 9:
                c = P["bois_fonce"]
            if planche == 1 and x % 13 == 5 and (y - 2) % 10 in (3, 4):
                c = P["bois_fonce"]  # nœud du bois
            px(img, x, y, c)
    for nx, ny in [(5, 5), (l - 6, 5), (5, 28), (l - 6, 28)]:
        px(img, nx, ny, P["bois_tres_fonce"])
    contour(img, P["bois_tres_fonce"])
    texte = str(nombre)
    ech = 3
    largeur = len(texte) * 5 * ech + (len(texte) - 1) * ech
    x0 = (l - largeur) // 2
    for i, car in enumerate(texte):
        glyphe(img, car, x0 + i * 6 * ech, 6, ech, P["texte"], P["creme"])
    return img


def gemme(couleur, taille):
    base, clair, fonce, eclat = (hexa(c) for c in COULEURS_GEMME[couleur])
    s = taille
    img = nouvelle(s, s)
    c = (s - 1) / 2
    haut = s * 0.32
    for y in range(s):
        for x in range(s):
            dx = x - c
            if y < haut:
                # Couronne (partie haute, trapèze)
                t = y / haut
                demi = s * 0.28 + t * s * 0.2
                if abs(dx) <= demi and y >= 1:
                    facette = (dx < -demi / 3, dx > demi / 3)
                    col = clair if facette[0] else (fonce if facette[1] else base)
                    if y < 2:
                        col = eclat
                    px(img, x, y, col)
            else:
                # Pavillon (pointe vers le bas)
                t = (y - haut) / (s - 1 - haut)
                demi = (s * 0.48) * (1 - t)
                if abs(dx) <= demi:
                    col = base if abs(dx) < demi * 0.35 else (clair if dx < 0 else fonce)
                    px(img, x, y, col)
    # Reflet
    px(img, c - s * 0.15, s * 0.42, eclat)
    px(img, c - s * 0.2, s * 0.5, eclat)
    contour(img, hexa("#1a0d12"))
    return img


def creux_gemme():
    img = nouvelle(16, 16)
    ellipse(img, 7.5, 7.5, 7.5, 7.5, P["pierre_fonce"])
    ellipse(img, 7.5, 7.5, 6, 6, P["pierre"])
    ellipse(img, 7.5, 8, 4, 4, hexa("#232633"))
    px(img, 4, 4, P["pierre_clair"])
    px(img, 5, 3, P["pierre_clair"])
    return img


def lumi(etat):
    img = nouvelle(15, 13)
    # Ailes
    if etat == "dort":
        ellipse(img, 4, 6, 2.5, 3.5, P["aile_fonce"])
        ellipse(img, 10, 6, 2.5, 3.5, P["aile_fonce"])
    elif etat == "vol1":
        ellipse(img, 3.5, 3.5, 3, 3.5, P["aile"])
        ellipse(img, 10.5, 3.5, 3, 3.5, P["aile"])
    else:
        ellipse(img, 3, 6, 3, 2.5, P["aile"])
        ellipse(img, 11, 6, 3, 2.5, P["aile"])
    # Corps lumineux
    ellipse(img, 7, 7.5, 3.6, 3.6, P["lumi"])
    ellipse(img, 7, 7.5, 2, 2, P["lumi_coeur"])
    # Yeux
    if etat == "dort":
        px(img, 6, 7, P["texte"])
        px(img, 8, 7, P["texte"])
        px(img, 5, 7, P["texte"])
        px(img, 9, 7, P["texte"])
    else:
        px(img, 6, 7, P["texte"])
        px(img, 8, 7, P["texte"])
        px(img, 7, 9, hexa("#ff8a80"))
    # Petites antennes
    px(img, 6, 3, P["lumi"])
    px(img, 8, 3, P["lumi"])
    px(img, 5, 2, P["lumi_coeur"])
    px(img, 9, 2, P["lumi_coeur"])
    return img


def portrait(initiale, couleur):
    base = hexa(couleur)
    clair = tuple(min(255, int(v + (255 - v) * 0.45)) for v in base[:3]) + (255,)
    fonce = tuple(int(v * 0.6) for v in base[:3]) + (255,)
    img = nouvelle(32, 32)
    ellipse(img, 15.5, 15.5, 15.5, 15.5, P["creme"])
    ellipse(img, 15.5, 15.5, 13.5, 13.5, None,
            lambda dx, dy: clair if dx + dy < -0.9 else (base if dx + dy < 0.8 else fonce))
    glyphe(img, initiale, 9, 6, 3, P["creme"], fonce)
    return img


def anneau_actif():
    img = nouvelle(40, 40)
    for y in range(40):
        for x in range(40):
            d = math.hypot(x + 0.5 - 20, y + 0.5 - 20)
            if 17 <= d <= 19.5:
                px(img, x, y, (255, 245, 157, 255 if d < 18.8 else 150))
    for a in range(0, 360, 45):
        ang = math.radians(a)
        px(img, 20 + math.cos(ang) * 19.5, 20 + math.sin(ang) * 19.5, P["blanc"])
    return img


def bouton_rond(taille, dessin):
    img = nouvelle(taille, taille)
    r = taille / 2
    ellipse(img, r - 0.5, r + 0.5, r - 0.5, r - 1, P["creme_ombre"])
    ellipse(img, r - 0.5, r - 0.5, r - 0.5, r - 1.5, P["creme"])
    dessin(img, r)
    contour(img, P["texte"])
    return img


def bouton_jouer():
    def d(img, r):
        for y in range(int(r - 9), int(r + 10)):
            t = (y - (r - 9)) / 19
            larg = int(16 * (1 - abs(t - 0.5) * 2))
            for x in range(int(r - 6), int(r - 6) + larg):
                px(img, x, y, P["hibou_fonce"])
    return bouton_rond(40, d)


OREILLE = [
    "....#####.....",
    "..##.....##...",
    ".#.........#..",
    "#...####....#.",
    "#..#....#...#.",
    "#..#.....#..#.",
    "#...#....#..#.",
    ".#...####..#..",
    ".#........#...",
    "..#......#....",
    "..#.....#.....",
    "...#...#......",
    "...#..#.......",
    "....##........",
]


def triangle(img, a, b, c, couleur):
    xs = [a[0], b[0], c[0]]
    ys = [a[1], b[1], c[1]]
    for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
        for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
            p = (x + 0.5, y + 0.5)

            def cote(p1, p2):
                return (p[0] - p2[0]) * (p1[1] - p2[1]) - (p1[0] - p2[0]) * (p[1] - p2[1])
            d1, d2, d3 = cote(a, b), cote(b, c), cote(c, a)
            neg = d1 < 0 or d2 < 0 or d3 < 0
            pos = d1 > 0 or d2 > 0 or d3 > 0
            if not (neg and pos):
                px(img, x, y, couleur)


def bouton_oreille():
    def d(img, r):
        c = P["hibou_fonce"]
        x0, y0 = int(r - 11), int(r - 7)
        # Pavillon de l'oreille couleur peau, puis trait épais
        ellipse(img, x0 + 6.5, y0 + 5.5, 5.5, 5, P["peau"])
        for gy, rang in enumerate(OREILLE):
            for gx, b in enumerate(rang):
                if b == "#":
                    px(img, x0 + gx, y0 + gy, c)
                    px(img, x0 + gx + 1, y0 + gy, c)
        # Ondes sonores
        for rayon in (3, 6):
            for a in range(-55, 56, 3):
                ang = math.radians(a)
                px(img, r + 3 + math.cos(ang) * rayon, r - 3 + math.sin(ang) * rayon, P["oiseau_fonce"])
                px(img, r + 3 + math.cos(ang) * (rayon + 0.8), r - 3 + math.sin(ang) * (rayon + 0.8), P["oiseau_fonce"])
    return bouton_rond(28, d)


def bouton_recommencer():
    def d(img, r):
        c = P["texte"]
        cx, cy = r - 0.5, r - 0.5
        for a in range(70, 360, 2):
            ang = math.radians(a)
            for e in (6.5, 7.5, 8.5):
                px(img, cx + math.cos(ang) * e, cy - math.sin(ang) * e, c)
        # Pointe de flèche au bout de l'arc (en haut), tournée vers la droite
        ang = math.radians(70)
        ex, ey = cx + math.cos(ang) * 7.5, cy - math.sin(ang) * 7.5
        triangle(img, (ex - 1, ey - 5.5), (ex - 1, ey + 5.5), (ex + 6, ey), c)
    return bouton_rond(32, d)


def engrenage():
    img = nouvelle(15, 15)
    for a in range(0, 360, 45):
        ang = math.radians(a)
        rect(img, 7 + math.cos(ang) * 6 - 1, 7 + math.sin(ang) * 6 - 1,
             7 + math.cos(ang) * 6, 7 + math.sin(ang) * 6, P["creme_ombre"])
    ellipse(img, 7, 7, 5, 5, P["creme_ombre"])
    ellipse(img, 7, 7, 2, 2, TRANSPARENT)
    contour(img, hexa("#4a4a4a"))
    return img


def etincelle():
    img = nouvelle(7, 7)
    for i in range(7):
        a = 255 if abs(i - 3) < 2 else 170
        px(img, 3, i, (255, 255, 255, a))
        px(img, i, 3, (255, 255, 255, a))
    return img


def pixel_blanc():
    img = nouvelle(4, 4)
    rect(img, 0, 0, 3, 3, P["blanc"])
    return img


def halo():
    img = nouvelle(32, 32)
    for y in range(32):
        for x in range(32):
            d = math.hypot(x + 0.5 - 16, y + 0.5 - 16) / 16
            if d < 1:
                # Halo en paliers (pas de dégradé lisse : on reste pixel art)
                a = 90 if d < 0.35 else (55 if d < 0.65 else 25)
                px(img, x, y, (255, 250, 200, a))
    return img


def bulle_zz():
    img = nouvelle(9, 9)
    for (x0, y0, s) in [(0, 4, 4), (5, 0, 3)]:
        rect(img, x0, y0, x0 + s, y0, P["creme"])
        ligne(img, x0 + s, y0, x0, y0 + s, P["creme"])
        rect(img, x0, y0 + s, x0 + s, y0 + s, P["creme"])
    return img


# ─── Programme principal ──────────────────────────────────────────────────────


def echelles_du_decor():
    """Lit le tableau JSON pour dessiner chaque élément du décor à sa vraie taille."""
    vues = set()
    try:
        scene = json.loads(SCENE.read_text(encoding="utf-8"))
        for d in scene.get("decor", []):
            vues.add((d["type"], round(float(d.get("scale", 1)), 2)))
    except FileNotFoundError:
        pass
    for t in ("tree", "pine", "bush", "rock"):
        vues.add((t, 1.0))
    return vues


def main():
    fond_foret()
    fabriques = {"tree": arbre, "pine": sapin, "bush": buisson, "rock": rocher}
    for type_, ech in sorted(echelles_du_decor()):
        if type_ in fabriques:
            sauver(fabriques[type_](ech), f"decor_{type_}_{int(round(ech * 100))}")
    sauver(touffe(), "decor_grass_100")
    sauver(branche(), "decor_branch")
    sauver(hibou(False), "anim_hibou_0")
    sauver(hibou(True), "anim_hibou_1")
    for i, e in enumerate(["pose", "vol1", "vol2"]):
        sauver(oiseau(e), f"anim_oiseau_{i}")
    sauver(fleur(False), "anim_fleur_0")
    sauver(fleur(True), "anim_fleur_1")
    for c in COULEURS_CHAMPIGNON:
        sauver(champignon(c), f"champignon_{c}")
    sauver(panier("dos"), "panier_dos")
    sauver(panier("face"), "panier_face")
    for n in range(0, 11):
        sauver(panneau(n), f"panneau_{n}")
    for c in COULEURS_GEMME:
        sauver(gemme(c, 21), f"gemme_{c}")
        sauver(gemme(c, 11), f"gemme_{c}_petite")
    sauver(creux_gemme(), "creux_gemme")
    sauver(lumi("vol0"), "lumi_0")
    sauver(lumi("vol1"), "lumi_1")
    sauver(lumi("dort"), "lumi_dort")
    sauver(portrait("E", "#ec407a"), "portrait_elea")
    sauver(portrait("I", "#1e88e5"), "portrait_isao")
    sauver(anneau_actif(), "anneau_actif")
    sauver(bouton_jouer(), "bouton_jouer")
    sauver(bouton_oreille(), "bouton_oreille")
    sauver(bouton_recommencer(), "bouton_recommencer")
    sauver(engrenage(), "bouton_reglages")
    sauver(etincelle(), "fx_etincelle")
    sauver(pixel_blanc(), "fx_pixel")
    sauver(halo(), "fx_halo")
    sauver(bulle_zz(), "fx_zz")
    print(f"Images écrites dans {SORTIE}")


if __name__ == "__main__":
    main()
