<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'

interface SavedQuery {
  id: number
  label: string
  query: string
  sql: string
  applicationId: number
  eventId: number
  userId: string
  hasData: boolean
  thumbsUp: boolean | null
  tokensUsed: number
  createdAt: string
  lastUsed: string | null
  useCount: number
}

interface Props {
  currentQuery?: string
  currentSql?: string
  currentApplicationId?: number
  currentEventId?: number
  currentUserId?: string
  currentHasData?: boolean
  currentTokensUsed?: number
}

interface Emits {
  (e: 'select-query', query: string): void
  (e: 'feedback-submitted', queryId: number, feedback: boolean): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// State
const savedQueries = ref<SavedQuery[]>([])
const showSaveDialog = ref(false)
const showRepository = ref(false)
const loading = ref(false)
const searchTerm = ref('')
const filterBy = ref('all') // all, successful, failed, recent

// Save dialog state
const saveLabel = ref('')
const saveLoading = ref(false)

// Computed
const filteredQueries = computed(() => {
  let filtered = savedQueries.value

  // Apply search filter
  if (searchTerm.value.trim()) {
    const search = searchTerm.value.toLowerCase()
    filtered = filtered.filter(q => 
      q.label.toLowerCase().includes(search) ||
      q.query.toLowerCase().includes(search) ||
      q.sql.toLowerCase().includes(search)
    )
  }

  // Apply category filter
  switch (filterBy.value) {
    case 'successful':
      filtered = filtered.filter(q => q.hasData)
      break
    case 'failed':
      filtered = filtered.filter(q => !q.hasData)
      break
    case 'recent':
      filtered = filtered.filter(q => {
        const dayAgo = new Date()
        dayAgo.setDate(dayAgo.getDate() - 1)
        return new Date(q.createdAt) > dayAgo
      })
      break
    case 'liked':
      filtered = filtered.filter(q => q.thumbsUp === true)
      break
    case 'disliked':
      filtered = filtered.filter(q => q.thumbsUp === false)
      break
  }

  // Sort by most recent first
  return filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
})

const repositoryStats = computed(() => {
  const total = savedQueries.value.length
  const successful = savedQueries.value.filter(q => q.hasData).length
  const liked = savedQueries.value.filter(q => q.thumbsUp === true).length
  const totalTokens = savedQueries.value.reduce((sum, q) => sum + q.tokensUsed, 0)
  
  return { total, successful, liked, totalTokens }
})

// Methods
const loadSavedQueries = async () => {
  try {
    loading.value = true
    // In a real app, this would call your API
    // const response = await axios.get('/api/saved-queries')
    // savedQueries.value = response.data
    
    // For demo, load from localStorage
    const saved = localStorage.getItem('savedQueries')
    if (saved) {
      savedQueries.value = JSON.parse(saved)
    }
  } catch (error) {
    console.error('Failed to load saved queries:', error)
  } finally {
    loading.value = false
  }
}

const saveCurrentQuery = async () => {
  if (!saveLabel.value.trim() || !props.currentQuery || !props.currentSql) {
    return
  }

  try {
    saveLoading.value = true
    
    const newQuery: SavedQuery = {
      id: Date.now(), // In real app, this would be from the server
      label: saveLabel.value.trim(),
      query: props.currentQuery,
      sql: props.currentSql,
      applicationId: props.currentApplicationId || 1,
      eventId: props.currentEventId || 1,
      userId: props.currentUserId || 'user123',
      hasData: props.currentHasData || false,
      thumbsUp: null,
      tokensUsed: props.currentTokensUsed || 0,
      createdAt: new Date().toISOString(),
      lastUsed: null,
      useCount: 0
    }

    // In a real app, this would call your API
    // await axios.post('/api/saved-queries', newQuery)
    
    // For demo, save to localStorage
    savedQueries.value.unshift(newQuery)
    localStorage.setItem('savedQueries', JSON.stringify(savedQueries.value))
    
    // Reset form
    saveLabel.value = ''
    showSaveDialog.value = false
    
    console.log('Query saved successfully!')
  } catch (error) {
    console.error('Failed to save query:', error)
  } finally {
    saveLoading.value = false
  }
}

const selectQuery = async (query: SavedQuery) => {
  try {
    // Update usage stats
    query.lastUsed = new Date().toISOString()
    query.useCount++
    
    // In real app, update on server
    // await axios.patch(`/api/saved-queries/${query.id}`, { lastUsed: query.lastUsed, useCount: query.useCount })
    
    // Update localStorage
    localStorage.setItem('savedQueries', JSON.stringify(savedQueries.value))
    
    emit('select-query', query.query)
  } catch (error) {
    console.error('Failed to update query usage:', error)
    // Still emit the query even if update fails
    emit('select-query', query.query)
  }
}

const submitFeedback = async (queryId: number, isPositive: boolean) => {
  try {
    const query = savedQueries.value.find(q => q.id === queryId)
    if (!query) return
    
    query.thumbsUp = isPositive
    
    // In real app, send to server
    // await axios.patch(`/api/saved-queries/${queryId}/feedback`, { thumbsUp: isPositive })
    
    // Update localStorage
    localStorage.setItem('savedQueries', JSON.stringify(savedQueries.value))
    
    emit('feedback-submitted', queryId, isPositive)
  } catch (error) {
    console.error('Failed to submit feedback:', error)
  }
}

const deleteQuery = async (queryId: number) => {
  if (!confirm('Are you sure you want to delete this saved query?')) return
  
  try {
    // In real app, delete from server
    // await axios.delete(`/api/saved-queries/${queryId}`)
    
    savedQueries.value = savedQueries.value.filter(q => q.id !== queryId)
    localStorage.setItem('savedQueries', JSON.stringify(savedQueries.value))
  } catch (error) {
    console.error('Failed to delete query:', error)
  }
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const canSaveCurrentQuery = computed(() => {
  return props.currentQuery && props.currentSql && saveLabel.value.trim()
})

// Lifecycle
onMounted(() => {
  loadSavedQueries()
})
</script>

<template>
  <div class="space-y-4">
    <!-- Save Current Query Button -->
    <div v-if="currentQuery && currentSql" class="card">
      <div class="flex items-center justify-between mb-3">
        <h4 class="text-sm font-semibold text-slate-800 flex items-center">
          <svg class="w-4 h-4 mr-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
          </svg>
          Save Current Query
        </h4>
      </div>
      
      <div v-if="!showSaveDialog" class="text-center">
        <button 
          @click="showSaveDialog = true"
          class="btn-primary text-sm w-full"
        >
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3-3m0 0l-3 3m3-3v12"/>
          </svg>
          Save to Repository
        </button>
      </div>

      <!-- Save Dialog -->
      <div v-if="showSaveDialog" class="space-y-3">
        <div>
          <label class="block text-xs font-medium text-slate-700 mb-1">Label for this query:</label>
          <input
            v-model="saveLabel"
            type="text"
            placeholder="e.g., Top 20 participants with A names"
            class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
            maxlength="100"
          />
          <div class="text-xs text-slate-500 mt-1">{{ saveLabel.length }}/100</div>
        </div>
        
        <div class="flex gap-2">
          <button
            @click="saveCurrentQuery"
            :disabled="!canSaveCurrentQuery || saveLoading"
            class="btn-primary text-sm flex-1"
          >
            <span v-if="saveLoading" class="flex items-center justify-center">
              <div class="loading-spinner mr-2"></div>
              Saving...
            </span>
            <span v-else>Save Query</span>
          </button>
          
          <button
            @click="showSaveDialog = false; saveLabel = ''"
            class="btn-secondary text-sm"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>

    <!-- Repository Section -->
    <div class="card">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-lg font-semibold text-slate-800 flex items-center">
          <svg class="w-5 h-5 mr-2 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/>
          </svg>
          Query Repository
        </h3>
        <button
          @click="showRepository = !showRepository"
          class="text-purple-500 hover:text-purple-600 transition-colors"
        >
          <svg 
            class="w-5 h-5 transform transition-transform"
            :class="{ 'rotate-180': showRepository }"
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </button>
      </div>

      <!-- Repository Stats -->
      <div class="grid grid-cols-2 gap-3 mb-4">
        <div class="p-3 bg-purple-50 border border-purple-200 rounded-lg text-center">
          <p class="text-xs font-medium text-purple-800">Saved Queries</p>
          <p class="text-lg font-bold text-purple-600">{{ repositoryStats.total }}</p>
        </div>
        
        <div class="p-3 bg-green-50 border border-green-200 rounded-lg text-center">
          <p class="text-xs font-medium text-green-800">Successful</p>
          <p class="text-lg font-bold text-green-600">{{ repositoryStats.successful }}</p>
        </div>
      </div>

      <Transition name="slide">
        <div v-if="showRepository" class="space-y-4">
          <!-- Search and Filters -->
          <div class="space-y-3">
            <div class="relative">
              <input
                v-model="searchTerm"
                type="text"
                placeholder="Search saved queries..."
                class="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-purple-500 outline-none pl-8"
              />
              <svg class="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>
            </div>

            <div class="flex flex-wrap gap-2">
              <button
                v-for="filter in [
                  { value: 'all', label: 'All', icon: '📋' },
                  { value: 'successful', label: 'Successful', icon: '✅' },
                  { value: 'failed', label: 'Failed', icon: '❌' },
                  { value: 'recent', label: 'Recent', icon: '🕒' },
                  { value: 'liked', label: 'Liked', icon: '👍' },
                  { value: 'disliked', label: 'Disliked', icon: '👎' }
                ]"
                :key="filter.value"
                @click="filterBy = filter.value"
                class="text-xs px-2 py-1 rounded-lg transition-all"
                :class="filterBy === filter.value 
                  ? 'bg-purple-500 text-white' 
                  : 'bg-slate-100 text-slate-700 hover:bg-purple-100'"
              >
                {{ filter.icon }} {{ filter.label }}
              </button>
            </div>
          </div>

          <!-- Saved Queries List -->
          <div class="space-y-3 max-h-96 overflow-y-auto">
            <div v-if="loading" class="text-center py-8">
              <div class="loading-spinner mx-auto mb-2"></div>
              <p class="text-sm text-slate-600">Loading saved queries...</p>
            </div>

            <div v-else-if="filteredQueries.length === 0" class="text-center py-8 text-slate-500">
              <svg class="w-12 h-12 mx-auto mb-3 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
              <p class="text-sm">No saved queries found</p>
              <p class="text-xs text-slate-400 mt-1">Save your first query to get started</p>
            </div>

            <div
              v-for="query in filteredQueries"
              :key="query.id"
              class="group p-4 bg-slate-50 hover:bg-purple-50 rounded-lg border border-slate-200 hover:border-purple-300 transition-all duration-200"
            >
              <!-- Query Header -->
              <div class="flex items-start justify-between mb-3">
                <div class="flex-1 min-w-0">
                  <h5 class="text-sm font-semibold text-slate-800 truncate">{{ query.label }}</h5>
                  <div class="flex items-center gap-3 mt-1 text-xs text-slate-500">
                    <span>{{ formatDate(query.createdAt) }}</span>
                    <span v-if="query.useCount > 0">Used {{ query.useCount }}x</span>
                    <span class="flex items-center">
                      <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                      </svg>
                      {{ query.tokensUsed }} tokens
                    </span>
                  </div>
                </div>
                
                <!-- Status Indicators -->
                <div class="flex items-center gap-2">
                  <span v-if="query.hasData" class="text-green-500 text-xs">✅</span>
                  <span v-else class="text-red-500 text-xs">❌</span>
                  
                  <span v-if="query.thumbsUp === true" class="text-green-500 text-xs">👍</span>
                  <span v-else-if="query.thumbsUp === false" class="text-red-500 text-xs">👎</span>
                </div>
              </div>

              <!-- Query Text -->
              <div class="mb-3">
                <p class="text-xs text-slate-600 line-clamp-2 mb-2">{{ query.query }}</p>
                <details class="text-xs">
                  <summary class="cursor-pointer text-purple-600 hover:text-purple-700">View SQL</summary>
                  <pre class="mt-2 p-2 bg-slate-800 text-slate-100 rounded text-xs overflow-x-auto">{{ query.sql }}</pre>
                </details>
              </div>

              <!-- Actions -->
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <button
                    @click="selectQuery(query)"
                    class="text-xs bg-purple-500 hover:bg-purple-600 text-white px-3 py-1 rounded-lg transition-colors"
                  >
                    Use Query
                  </button>
                  
                  <!-- Feedback Buttons -->
                  <div class="flex items-center gap-1">
                    <button
                      @click="submitFeedback(query.id, true)"
                      class="p-1 rounded transition-colors"
                      :class="query.thumbsUp === true 
                        ? 'bg-green-100 text-green-600' 
                        : 'text-slate-400 hover:text-green-500'"
                    >
                      👍
                    </button>
                    <button
                      @click="submitFeedback(query.id, false)"
                      class="p-1 rounded transition-colors"
                      :class="query.thumbsUp === false 
                        ? 'bg-red-100 text-red-600' 
                        : 'text-slate-400 hover:text-red-500'"
                    >
                      👎
                    </button>
                  </div>
                </div>

                <button
                  @click="deleteQuery(query.id)"
                  class="text-slate-400 hover:text-red-500 transition-colors"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </div>
  </div>
</template>