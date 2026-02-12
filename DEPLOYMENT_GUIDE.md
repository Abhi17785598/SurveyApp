# 🚀 Deploy Survey Web App to Render - Complete Guide

## 📋 Prerequisites
- GitHub account
- Render account (free tier available)
- Your SurveyWebApp code pushed to GitHub

## 🔧 Step-by-Step Deployment

### 1. Push Your Code to GitHub
```bash
git init
git add .
git commit -m "Initial commit - Survey Web App ready for deployment"
git branch -M main
git remote add origin https://github.com/yourusername/survey-web-app.git
git push -u origin main
```

### 2. Create Render Account
1. Go to [render.com](https://render.com)
2. Sign up with GitHub
3. Verify your email

### 3. Deploy to Render

#### Option A: Automatic Deployment (Recommended)
1. Go to your Render dashboard
2. Click **"New +"** → **"Web Service"**
3. Connect your GitHub repository
4. Select the `survey-web-app` repository
5. Render will automatically detect your `render.yaml` file
6. Click **"Create Web Service"**

#### Option B: Manual Configuration
1. Click **"New +"** → **"Web Service"**
2. Connect your GitHub repository
3. Configure settings:
   - **Name**: survey-web-app
   - **Environment**: Docker
   - **Region**: Oregon (or closest to you)
   - **Plan**: Free
   - **Dockerfile Path**: `./Dockerfile`

### 4. Database Setup
1. In your Render dashboard, click **"New +"** → **"PostgreSQL"**
2. Configure:
   - **Name**: survey-db
   - **Database Name**: surveydb
   - **User**: surveyuser
   - **Plan**: Free
3. After creation, go to the database dashboard
4. Copy the **External Database URL**

### 5. Connect Database to Web App
1. Go to your web service dashboard
2. Click **"Environment"** tab
3. Add environment variable:
   - **Key**: `DATABASE_URL`
   - **Value**: (paste the External Database URL from step 4)
4. Click **"Save Changes"**
5. Click **"Manual Deploy"** → **"Deploy Latest Commit"**

### 6. Run Database Migrations
Your app will need to create the database schema. The first deployment might fail if the database tables don't exist. You can:

1. **Option 1**: Add automatic migration in your `Program.cs`
2. **Option 2**: Use Render's shell to run migrations manually

## 🔍 Verify Deployment
1. Wait for deployment to complete (green status)
2. Click on your service URL
3. Test the health endpoint: `https://your-app-url.onrender.com/health`
4. Should return: `{"status":"healthy","timestamp":"..."}`

## 🐛 Troubleshooting

### Common Issues & Solutions

#### 1. Build Fails
- Check Dockerfile syntax
- Verify .csproj file exists
- Check render.yaml configuration

#### 2. Database Connection Error
- Verify DATABASE_URL environment variable
- Check if database is running
- Ensure connection string format is correct

#### 3. Health Check Fails
- Ensure HealthController.cs is included
- Check if port 8080 is exposed in Dockerfile
- Verify app is listening on correct port

#### 4. 502 Bad Gateway
- Check if app started successfully
- Review deployment logs
- Verify health check endpoint

## 📊 Monitoring
- Check logs in Render dashboard
- Monitor resource usage
- Set up alerts if needed

## 🔄 Updates
- Push changes to GitHub
- Render auto-deploys (if enabled)
- Or trigger manual deploy

## 💡 Pro Tips
1. Use environment variables for sensitive data
2. Enable automatic deployments for convenience
3. Monitor free tier limits
4. Set up custom domain when ready

## 🆘 Support
- Render documentation: docs.render.com
- .NET on Render guide
- Community forum: community.render.com

---

**🎉 Your Survey Web App is now live on Render!**
