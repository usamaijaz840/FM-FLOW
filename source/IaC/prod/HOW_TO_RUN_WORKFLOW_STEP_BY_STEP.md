# How to Run Workflow Manually - Step by Step Guide

**Date:** January 5, 2026

---

## 🎯 Important: "Latest #3" Dropdown Kya Hai?

**"Latest #3" dropdown = Previous attempts ki list**
- Yeh dropdown sirf **purane runs** dikhata hai
- Isse aap **purane attempts** dekh sakte hain
- **Yeh "Run workflow" button nahi hai!**

---

## ✅ Correct Way: "Run workflow" Button Kahan Hai?

### Method 1: Workflow List Page Se

1. **GitHub Actions Tab** par jao
2. **Left sidebar** mein "Workflows" section dekho
3. **"Deploy to Production Environment"** workflow click karo
4. **Top right corner** mein **"Run workflow"** button dikhega (blue/green button)
5. Us button par click karo

### Method 2: Current Run Page Se

1. **Current page** par ho (jahan "Latest #3" dropdown hai)
2. **Top right corner** mein dekho:
   - **"Re-run all jobs"** button (yeh nahi)
   - **"Run workflow"** button (yeh hai!) - usually **dropdown ke upar** ya **side mein**

---

## 📋 Complete Step-by-Step Process

### Step 1: Navigate to Workflow
```
GitHub → Actions Tab → "Deploy to Production Environment" workflow
```

### Step 2: Find "Run workflow" Button
- **Top right corner** mein dekho
- Button text: **"Run workflow"** (blue/green color)
- **NOT** "Re-run all jobs" (yeh alag hai)

### Step 3: Click "Run workflow"
- Button click karo
- Ek **dropdown menu** khulega

### Step 4: Select Branch
- Dropdown mein **branch select** karo:
  - Usually: `main` ya `production`
  - Dropdown mein available branches dikhengi

### Step 5: Fill Inputs
Dropdown menu mein **3 inputs** honge:

1. **`deploy_backend`**
   - Type: Checkbox
   - ✅ **Tick karo** (true)

2. **`deploy_frontend`**
   - Type: Checkbox
   - ✅ **Tick karo** (true)

3. **`confirm_production`**
   - Type: Text field
   - **Type:** `DEPLOY` (exactly, capital letters)

### Step 6: Click "Run workflow" Button
- Dropdown ke **bottom** mein **"Run workflow"** button hoga
- Click karo

---

## 🖼️ Visual Guide

### Where to Find "Run workflow" Button:

```
┌─────────────────────────────────────────────┐
│  GitHub Actions                             │
│  ← Deploy to Production Environment         │
│                                              │
│  Merge pull request #900...                 │
│                                              │
│  [Summary] [All jobs]                       │
│                                              │
│  ┌─────────────────────────────────────┐   │
│  │  Workflow details...                 │   │
│  │                                      │   │
│  └─────────────────────────────────────┘   │
│                                              │
│                    [Re-run all jobs]  [Run workflow] ← YEH HAI!
│                    [Latest #3 ▼]            │
└─────────────────────────────────────────────┘
```

---

## ⚠️ Common Mistakes

### ❌ Wrong:
- "Latest #3" dropdown use karna (yeh purane runs dikhata hai)
- "Re-run all jobs" button use karna (yeh current run ko re-run karta hai)

### ✅ Correct:
- **"Run workflow"** button use karna (yeh naya manual run start karta hai)

---

## 🔍 If You Can't Find "Run workflow" Button

### Check Permissions:
- Aapke paas **write access** hona chahiye repository mein
- Agar nahi hai, to admin se request karo

### Alternative: Direct URL
```
https://github.com/fm-flow/fm-flow/actions/workflows/deploy-to-prod.yml
```
Is URL par jao, wahan **"Run workflow"** button dikhega.

---

## 📝 Summary

| Item | Location | Purpose |
|------|----------|---------|
| **"Run workflow"** | Top right corner | **Naya manual run start karta hai** |
| **"Re-run all jobs"** | Top right corner | Current run ko re-run karta hai |
| **"Latest #3" dropdown** | Right sidebar | Purane attempts dikhata hai |

---

## 🎯 Quick Checklist

- [ ] GitHub Actions tab open kiya
- [ ] "Deploy to Production Environment" workflow select kiya
- [ ] **"Run workflow"** button dhoondha (NOT "Re-run")
- [ ] Branch select kiya (`main` ya `production`)
- [ ] `deploy_backend`: ✅ Tick kiya
- [ ] `deploy_frontend`: ✅ Tick kiya
- [ ] `confirm_production`: `DEPLOY` type kiya
- [ ] "Run workflow" button click kiya

---

**Last Updated:** January 5, 2026


