# 🧠 Consultation Council Setup Guide

## Current Configuration

Your **Reasonix** now has a sophisticated **Consultation Council** setup:

### 🏆 **Implementation Model (Primary)**
- **Provider:** `custom-bedrock-mantle`
- **Model:** `minimax.minimax-m2` 
- **Use:** All implementation tasks automatically

```bash
# Uses your best model automatically
reasonix "Build a REST API with FastAPI"
reasonix "Fix the authentication bug"
```

### 🧠 **Planning Council Members**

#### 🎯 **qwen3-coder** - Architecture Expert
```bash
reasonix --model "qwen3-coder" "Design a scalable microservices architecture"
```

#### ⚡ **nemo-super-3** - Complex Analysis
```bash
reasonix --model "nemo-super-3" "Analyze performance bottlenecks in the system"
```

#### 📋 **kimi-k2.5** - Strategic Planning
```bash
reasonix --model "kimi-k2.5" "Create a project timeline for the migration"
```

## 🚀 **Recommended Workflow**

### **Phase 1: Planning with Council**
```bash
# Get architectural perspective
reasonix --model "qwen3-coder" "Plan the database schema and relationships"

# Analyze complex requirements  
reasonix --model "nemo-super-3" "Review security and compliance requirements"

# Create strategic plan
reasonix --model "kimi-k2.5" "Break down the project into phases"
```

### **Phase 2: Implementation (Auto-chooses best model)**
```bash
# Automatically uses minimax.minimax-m2
reasonix "Now implement the planned database schema"
reasonix "Build the authentication system with the planned security features"
reasonix "Create the migration scripts for each phase"
```

## ✅ **Benefits of This Setup**

- **🏆 Best quality implementation** with your strongest model
- **🧠 Diverse perspectives** for planning (avoids single-model bias)  
- **⚡ Flexible switching** based on task type
- **💰 Cost optimization** (use council models less frequently)
- **🎯 Consistent execution** with your best model for all code work

## 📝 **Quick Commands**

- **Default implementation:** `reasonix "your task here"`
- **Architecture planning:** `reasonix --model "qwen3-coder" "architecture task"`
- **Complex analysis:** `reasonix --model "nemo-super-3" "analysis task"`  
- **Strategic planning:** `reasonix --model "kimi-k2.5" "planning task"`

---
*Generated from your optimized reasonix.toml configuration*